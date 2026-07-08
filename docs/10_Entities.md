# C#-Modelle

## Domain-Entities

Die Domain ist nach Fachbereichen gegliedert:

- `Analytics`: `CalculationResult`, `BenchmarkDataset`
- `Buildings`: `Building` und Gebäude-Enums
- `Common`: `BaseEntity`
- `Customers`: `Customer`
- `Data`: `DataSource`
- `Documents`: `Document`
- `Energy`: `EnergySystem`, `Meter`, `MeterReading`
- `Geography`: `Region`, `Municipality`, `District`
- `Projects`: `Project`

`MeterReading` ist das einzige aktuelle Domainmodell ohne `BaseEntity`. `MeterNumber` ist die fachliche Zähleridentität; die geerbte GUID bleibt die technische Identität von `Meter`.

## Application-Prozessmodelle

Diese Modelle sind keine Domain-Entities:

- `ImportWorkbook`, `CustomerExcelRow`, `BuildingExcelRow`
- `CustomerImportDto`, `BuildingImportDto`, `MeterImportDto`, `MeterReadingImportDto`
- `ImportIssue` und `ImportIssueResolution`
- `ImportReport` und `ImportDecision`
- `ImportAuditEntry`, `ImportSourceFileMetadata` und `ImportWriteContext`
- `DuplicateCandidate<T>` und Merge-Hilfsmodelle
- `ImportJob` und `RawDataObject`

`DuplicateCandidate<T>` ist ein internes DuplicationCheck-Modell. Außerhalb des Moduls werden Auffälligkeiten ausschließlich als `ImportIssue` weitergegeben.

## Infrastructure-Typen

Excel-spezifische Workbook-Reader/-Writer und ClosedXML-Typen liegen in Infrastructure. Sie sind Adapter und keine fachlichen Entities.

## Bekannte Modelllücken

- relationales `ImportReport`-/ImportHistory-/Audit-Modell; aktuell besteht JSON-Persistenz;
- Data-Product-Verträge und Publikationsmetadaten;
- vollständige Provenance für Raw- und Curated-Daten;
- aktive Persistenz von `ImportJob` und `DataSource`;
- Namespaces für mehrere aktuell global definierte Domain-Enums.
