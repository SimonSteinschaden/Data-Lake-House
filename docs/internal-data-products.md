# Internal Data Products

## Fachliche Quelle

Seit Phase B beziehen `BuildingSummaryProduct`, `MeterSummaryProduct`,
`CustomerSummaryProduct` und `PortfolioSummaryProduct` sämtliche fachlichen
Werte aus Canonical Snapshots über `ICanonicalSnapshotReader`.

Direkte fachliche Projektionen aus Customer, Building, BuildingVersion, Meter,
MeterReading, EnergySystem oder `CuratedFieldValues` sind in den Product
Services nicht zulässig. `ImportQualityProduct` bleibt die dokumentierte
Ausnahme, da ImportReports, Issues und AuditTrail technische Importmetadaten
darstellen.

Quality Level und Suitability werden getrennt ausgegeben. Bronze- und
Silver-Snapshots bleiben vollständig in Bestandszahlen enthalten. Jahreswerte
werden ausschließlich aus der kanonischen Messwertzusammenfassung übernommen;
unvollständige Jahre liefern keinen Wert und werden nicht hochgerechnet.

Details: [Canonical Snapshots](canonical-snapshots.md) und
[ADR Canonical Snapshot](adr/ADR_CANONICAL_SNAPSHOT_AS_SINGLE_SOURCE_OF_TRUTH.md).

## Zweck und Abgrenzung

Internal Data Products sind unveränderliche, requestbasiert erzeugte fachliche
ReadModels für Dashboard und interne Detailansichten. Sie sind weder Domain
Entities noch persistierte Aggregate, Gold-Versionen, Exportverträge, externe
Data Products oder Marketplace-Produkte. Die Schicht schreibt keine Daten und
enthält keine eigene Import-, Curation-, Benchmark- oder Time-Series-Engine.

## Architektur

`Domain / ReadModels -> Gold Profiles -> Internal Data Products -> REST API ->
React Dashboard`

Die Contracts liegen in `Enset.Application/InternalDataProducts`, die
EF-Core-Projektionen in `Enset.Infrastructure/InternalDataProducts`. Alle
Abfragen verwenden `AsNoTracking`, den bestehenden mandantenabhängigen
`IDataAccessScope` und bestehende Gold-/Readiness-Projektionen.

## Product-Katalog und Datenquellen

- `BuildingSummaryProduct`: Building, aktuelle BuildingVersion, Address,
  CustomerBuildingAssignment, Meter, CurationTask, CuratedFieldValue,
  GoldProfileVersion und Data Product Readiness.
- `MeterSummaryProduct`: Meter, Building/Customer, aggregierte Metadaten der
  MeterReadings, Curation, Gold und Readiness.
- `CustomerSummaryProduct`: Customer, zugeordnete Buildings/Meter/EnergySystems,
  Curation und Gold.
- `PortfolioSummaryProduct`: sichtbare Customers, Buildings und Meter,
  Jahreswerte, Curation, Gold sowie Import- und Issue-Zähler.
- `ImportQualityProduct`: ImportReport, ImportIssue, ImportAuditEntry, Curation
  und Gold.

## Berechnungs- und Einheitenregeln

Jahresenergie wird ausschließlich aus bereits vorhandenen `Meter.AnnualValue`
übernommen. Es gibt keine Hochrechnung aus Messwerten. Werte werden zunächst
nach Energieträger, Richtung und Einheit gruppiert. Eine Gesamtkennzahl wird nur
ausgegeben, wenn genau eine Einheit vorhanden ist; andernfalls ist sie `null`
und `INCOMPATIBLE_UNITS` wird gemeldet. Energieträger werden nicht umgerechnet.
Fehlende Werte sind `null` beziehungsweise `NotAvailable`, nie stillschweigend
Null.

Die Messprofilvollständigkeit nutzt nur vorhandene Zeitgrenzen und ein
vorhandenes Messintervall. Ohne seriös bestimmbares Intervall bleiben Expected,
Missing und Completeness `null`. Readiness beschreibt Voraussetzungen und wird
nicht als berechnetes Product-Ergebnis dargestellt. Nur explizit freigegebene
Gold-Versionen zählen als released.

## API

- `GET /api/v1/internal-data-products/buildings/{id}/summary`
- `GET /api/v1/internal-data-products/meters/{id}/summary`
- `GET /api/v1/internal-data-products/customers/{id}/summary`
- `GET /api/v1/internal-data-products/portfolio/summary`
- `GET /api/v1/internal-data-products/import-quality`

Die Endpunkte verwenden die bestehende `CustomerReader`-Policy und liefern für
einen unbekannten oder nicht sichtbaren Scope RFC-7807-ProblemDetails mit 404.
Die API ist intern und kein Public-/Marketplace-Vertrag.

## Dashboard

Das Dashboard lädt ausschließlich `PortfolioSummaryProduct` und
`ImportQualityProduct`. Fachliche Counts, Reife-, Qualitäts-, Import- und
Readiness-Kennzahlen werden nicht mehr aus CRUD-Endpunkten im Browser
zusammengesetzt.

### Dashboard 2.0

Das Cockpit gliedert sich in Portfolio, Energie, Datenqualität,
Data-Product-Readiness sowie Import- und Bearbeitungsstatus. Es verwendet
weiterhin genau zwei Hauptrequests. Karten navigieren zu Kunden, Objekten,
Zählpunkten, Datenkuration oder Importhistorie.

Jedes `EnergySummaryItem` wird mit Energieträger, Richtung, Jahreswert, Einheit
und Zählpunktanzahl angezeigt. Balken verschiedener Einheiten erhalten
getrennte Diagrammgruppen und niemals eine gemeinsame Achse.

`PortfolioReadinessSummary` liefert Status, Prozentwert, vorbereitete und
blockierte Scopes, zentrale Blocker, Empfehlungen und Detailnavigation. Die UI
bezeichnet diese Werte als Voraussetzungen und nicht als berechnete Ergebnisse.
Probleme werden über eine Handlungsübersicht mit Wirkung und zuständiger
Fachseite priorisiert.

Leere Portfolios, fehlende Jahreswerte, Gold-Profile und Readiness sowie
API-Fehler erhalten eigene fachliche Zustände. Details zur Seitenstruktur sind
in `docs/dashboard-2-0.md` dokumentiert.

## Bekannte Einschränkungen

- ImportReport enthält derzeit keinen verlässlichen Importtyp oder Ziel-
  Zählpunkt; diese Felder bleiben `null`.
- Eine belastbare Priorität für Curation Tasks fehlt. Der derzeitige
  High-Priority-Zähler verwendet keine erfundene Fachpriorität.
- Portfolio-Readiness folgt der bestehenden Readiness-Projektion und wird
  batchweise aus sichtbaren Scopes und freigegebenen Gold-Profilen erzeugt.
  EEG und P2P werden im Cockpit nicht gezeigt, solange Netz- und
  Tarifvoraussetzungen im MVP fehlen.
- Ohne eindeutiges Referenzjahr am vorhandenen Jahreswert ist der
  ReferencePeriod `null`.
- Es wurde bewusst kein Cache eingeführt.
# Gemeinsame CRUD-Projektionen

Die CRUD-Leseansichten erzeugen keine konkurrierenden Data Products. Sie
verwenden dieselben Canonical Snapshots wie diese Products. Jahreswerte,
Quality Level, Customer- und Building-Zuordnung sind damit identisch. Siehe
[Unified CRUD Read Models](unified-crud-read-models.md).

## Phase D – LEB

Der LEB-Export erzeugt keine parallele Product-Persistenz.
`LebExportDataset` verwendet dieselben Canonical Snapshots.
