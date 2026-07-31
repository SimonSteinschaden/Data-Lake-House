# Unified CRUD Read Models

## Ziel und Ist-Analyse

Vor Phase C las `EfEntityReadService` fachliche Werte direkt aus EF-Entities und
`CuratedFieldValues`. Listen, Details und Internal Data Products berechneten
insbesondere Datenreife und Jahreswerte auf unterschiedlichen Wegen. Das
Frontend stellte außerdem Kunde, Objekt, Messzeitraum und Leerwerte
uneinheitlich dar.

## Entscheidung

`ICanonicalSnapshotReader` ist die gemeinsame fachliche Lesequelle für
Customer, Building, Meter und Meter-Reading-Summaries:

```text
Relationales Write Model
        |
ICanonicalSnapshotReader
        |
        +-- CRUD-Listen und -Details
        +-- Internal Data Products
        +-- Dashboard
```

Die existierenden CRUD-REST-Contracts bleiben aus Kompatibilitätsgründen
erhalten und wurden additiv um stabile kanonische Felder ergänzt. Gemeinsame
Werte werden ausschließlich aus `CustomerCanonicalSnapshot`,
`BuildingCanonicalSnapshot`, `MeterCanonicalSnapshot` und
`CanonicalReadingSummary` abgebildet.

## Projektionen

- Customer: Nummer, Name, Ort, Gemeinde, Gebäude-/Zählpunktanzahl und
  `QualityLevel`.
- Building: Nummer, Name, Kunde, Adresse, Gemeinde, Typ, Nutzung, Flächen,
  Baujahr, Zählpunktanzahl und `QualityLevel`.
- Meter: originale `MeterNumber`, getrenntes `Name`, Gebäude, Kunde, Medium,
  Richtung, Quantity, Einheit und `QualityLevel`.
- Measurement Summary: `MeasurementCount`, `PeriodStart`, `PeriodEnd`,
  `ReadingType`, `IntervalSeconds`, `AnnualValue`, Einheit, Referenzjahr und
  `AnnualValueStatus`.

Listenfilter und Sortierung arbeiten auf denselben Snapshotwerten, die
ausgegeben werden. Rohmesswert-Pagination und Aggregation bleiben eine
technische, berechtigungsgesicherte EF-Abfrage; deren fachliche Metadaten
stammen aus dem Meter-Snapshot.

## Jahreswert und Qualität

Die zentrale Snapshotlogik liefert den Jahreswert. Unvollständige Jahre haben
Status `IncompleteYear` und keinen Jahreswert; es findet keine Hochrechnung
statt. CRUD-Service, Controller und React berechnen keinen Jahreswert.

`QualityLevel` ist die kanonische Bronze-/Silver-/Gold-Stufe. Suitability bleibt
separat und anwendungsfallspezifisch.

## Write Model und Aktualität

Create, Update und Delete schreiben weiterhin in die relationalen Entities.
Customer- und EnergySystem-Snapshots werden in der aktuellen Baseline nicht
materialisiert. `EfCanonicalSnapshotReader` erzeugt Snapshots bei jeder
Abfrage synchron aus dem committed relationalen Stand. Daher ist kein
zusätzlicher Refresh, keine zweite Snapshot-Persistenz und keine
verteilte Transaktion erforderlich. Nach erfolgreichem `SaveChanges` sieht die
nächste CRUD-, Product- oder Dashboard-Abfrage denselben neuen Stand.

## REST und UI

Die bestehenden REST-Felder bleiben kompatibel; neue eindeutige Felder sind
unter anderem `qualityLevel`, `municipalityId`, `municipality`,
`annualValueStatus`, `annualValueUnit` und `annualValueReferenceYear`.

Gemeinsame reine Anzeigeelemente liegen in
`components/domain/CanonicalDisplays.tsx`: Customer-, Building-, Meter-,
Quality-Level-, Jahreswert-, Zeitraum- und Leerwertdarstellung. Fehlende
fachliche Bezeichnungen werden nicht durch GUIDs ersetzt.

## Architekturregeln und Einschränkungen

- Keine fachlichen EF-Projektionen und keine Curated-Field-Zugriffe in
  CRUD-Listen.
- EF in Details nur für technische/auditbezogene Zusatzdaten und für
  Rohmesswerte.
- Keine zweite Product- oder Snapshot-Persistenz.
- EnergySystem bleibt außerhalb Phase C, da die Baseline keine entsprechende
  dauerhaft materialisierte Snapshotprojektion vorgibt.
- `includeDeleted` kann fachliche Snapshotwerte gelöschter Entitäten nicht
  liefern, solange der zentrale Reader Query-Filter anwendet.
- Der aktuelle On-demand-Portfolio-Reader ist fachlich konsistent; seine
  interne Bündelung bleibt ein späteres Performance-Thema.

## Phase D

Der LEB-Export verwendet ebenfalls diese Quelle; siehe
[LEB Export – Canonical Projection](leb-export-canonical-projection.md).
