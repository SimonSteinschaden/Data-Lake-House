# Association Management

## Zweck und Architektur

Die Seite **Zuordnungen** verwaltet fachliche Beziehungen über eine generische,
servergesteuerte Oberfläche. Fachlogik und Kompatibilitätsmatrix liegen im
Backend. Die Persistenz bleibt typisiert und verwendet echte Foreign Keys.
Controller und Frontend greifen nicht direkt auf Join-Tabellen zu.

## Kompatibilitätsmatrix

| Key | Quelle | Ziel | Kardinalität | Primär | Gültigkeit |
|---|---|---|---|---|---|
| `customer-building` | Kunde | Objekt | n:m | ja | ja |
| `building-meter` | Objekt | Zählpunkt | 1:n | ja | ja |
| `building-energy-system` | Objekt | Anlage | 1:n | ja | ja |
| `meter-series` | Zählpunkt | Messreihe | 1:1 | implizit | nein |
| `building-document` | Objekt | Dokument | n:m | nein | optional |
| `customer-project` | Kunde | Projekt | n:m | ja | optional |

Messreihen sind keine eigene Stammdatenentität. Sie sind durch
`MeterReading.MeterId` eindeutig an den internen Zählpunkt gebunden. Eine
abweichende Zuordnung ist blockiert; Messwerte werden weder kopiert noch
dupliziert.

## Fachliche Persistenz

- `CustomerBuildingAssignment` wird weiterverwendet.
- `EnergySystemBuildingAssignment` wird um `IsPrimary` ergänzt.
- `BuildingMeterAssignment`, `BuildingDocumentAssignment` und
  `CustomerProjectAssignment` ergänzen Historisierung, Rolle und Gültigkeit.
- `Meter.BuildingId` und `Project.CustomerId` bleiben als aktuelle Projektion
  bestehen und werden innerhalb derselben Transaktion synchronisiert.
- `AssociationAuditEntry` speichert den aggregierten Änderungsvorgang.

Eine universelle polymorphe Assignment-Tabelle wurde bewusst verworfen.

## Rollen

Rollen sind je Typ als Enum beziehungsweise serverseitig definierte Werteliste
festgelegt. Beispiele sind Eigentümer/Betreiber/Verwalter, Haupt- und
Unterzähler, Anlagenbeziehung, Dokumenttyp und Projektrolle. Freitext ist nur
als Begründung verfügbar.

## Vorschau, Konflikte und Transaktion

Jede Änderung beginnt mit `preview`. Geprüft werden Auswahl, Rolle,
Kardinalität, Gültigkeitsreihenfolge, aktive Referenzen, Duplikate,
Primärkonflikte und bereits belegte 1:n-Ziele. Konflikte sind `Information`,
`Warning` oder `Blocking`. Warnungen müssen explizit bestätigt werden;
Blocking verhindert Commit.

Commit, Primärwechsel und Beenden einer Beziehung sind pro Benutzeraktion
atomar. Es gibt keinen Partial-Success-Modus. Entfernen setzt ein
Gültigkeitsende; historische Zeilen werden nicht physisch gelöscht.

## Audit und Historie

Auditdatensätze enthalten OperationId, Benutzer, Zeitpunkt, Typ, Quelle, Ziel,
Aktion, Vorher/Nachher und Begründung. Primärwechsel schreiben sowohl
`UnmarkedPrimary` als auch `MarkedPrimary`.

## API

- `GET /api/v1/associations/types`
- `GET /api/v1/associations/entities`
- `GET /api/v1/associations`
- `POST /api/v1/associations/preview`
- `POST /api/v1/associations`
- `POST /api/v1/associations/remove-preview`
- `POST /api/v1/associations/remove`
- `PATCH /api/v1/associations/{id}/primary`
- `GET /api/v1/associations/history`

Entitätssuchen sind serverseitig paginiert, case-insensitive und verwenden
schlanke `AsNoTracking`-Projektionen mit aggregierten Assignment Counts.

## Canonical Snapshots

Customer/Building lesen die vorhandenen Customer-Zuordnungen,
Meter/Building die synchronisierte `Meter.BuildingId`-Projektion und
EnergySystem/Building die bestehende Assignment-Entity. Die deterministischen
Snapshots werden bei der nächsten Abfrage neu aufgebaut; es wird keine zweite
Snapshot-Persistenz eingeführt. Projekt- und Dokument-Snapshots existieren in
der Baseline nicht.

## Migration und Bestandsdaten

`AddAssociationManagement` erstellt die neuen typisierten Tabellen, Audit und
Indizes. Eindeutige bestehende Meter- und Projekt-FKs werden als primäre
Historienzeile übernommen. Das Migrationsdatum ist der technische
Historienbeginn; eine frühere fachliche Gültigkeit wird nicht erfunden.
Partielle eindeutige PostgreSQL-Indizes schützen aktive primäre Zählpunkt- und
Projektzuordnungen.

## UI und Performance

Die UI besitzt zwei unabhängige Suchlisten, Mehrfachauswahl gemäß Matrix,
Rollen- und Gültigkeitsfelder, Vorschau, Konflikte, bestehende Beziehungen und
Audit-Historie. Unter 900 Pixeln werden Quelle, Ziel und Aktionen untereinander
dargestellt.

## Bekannte Einschränkungen

- Projekt- und Dokumentnummern fehlen im aktuellen Domainmodell.
- Messreihe ist ein MeterReading-Aggregat, kein frei verschiebbares Objekt.
- Gültigkeitsüberschneidungen werden im MVP auf aktive Beziehungen und
  Primärkonflikte begrenzt; ein PostgreSQL-Exclusion-Constraint ist eine
  mögliche Erweiterung.
- Verantwortlichenzuweisung für Konflikte gehört in einen späteren Workflow.
- PostgreSQL-Volltext- oder Trigram-Indizes sind erst bei nachgewiesenem Bedarf
  vorgesehen.
