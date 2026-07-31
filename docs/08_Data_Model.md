# Technisches Daten- und Persistenzmodell

## BaseEntity

`BaseEntity` definiert:

- `Guid Id`, standardmäßig neu erzeugt;
- `DateTime CreatedAt` in UTC;
- optionales `DateTime UpdatedAt`.

`MeterReading` erbt bewusst nicht von `BaseEntity`.

## Aktive DbSets

`EnsetDbContext` registriert:

- `Customers`, `Projects`, `Buildings`;
- `EnergySystems`, `Meters`, `MeterReadings`;
- `Documents`;
- `CalculationResults`, `BenchmarkDatasets`.

`ImportJob` und `DataSource` sind modelliert, aber nicht als aktive DbSets eingebunden.

## Schlüssel und Indizes

- `MeterReading`: Composite Key aus `MeterId + Timestamp`;
- Index auf `MeterReading.Timestamp`;
- eindeutiger Index auf `Meter.MeterNumber`;
- Fremdschlüsselbeziehung `MeterReading.MeterId -> Meter`.

## Zeitreihen- und Provenance-Felder

`MeterReading` enthält Wert, Einheit und `DataQuality`. Optional sind `CustomerId`, `BuildingId` und `SourceImportJobId` vorhanden. Für diese optionalen Provenance-Felder ist derzeit keine vollständige Persistenz- und Importkette implementiert.

## Aktuelle Grenzen

- `ImportReport` und Audit Trail werden nur als JSON-Dateien, nicht relational persistiert;
- keine unveränderliche ImportHistory oder Audit-Entities in der Datenbank;
- `DatabaseImportWriter` ist vorhanden, blockiert aber sicher bis zum fachlichen Mapping;
- kein nachgewiesener TimescaleDB-Hypertable;
- keine Data-Product-Entities mit Version, Schema und Publikationsmetadaten.
# Stand 1.0 RC

Dieses Dokument ist ergänzend. Maßgeblich sind die Domainklassen, `EnsetDbContext` und [das aktuelle ER-Diagramm](UML/10_Database_ER.puml). DataProductDefinition, DataProduct, ScopeAssignment, GenerationRun, Version und Value sind inzwischen implementiert und migriert.
