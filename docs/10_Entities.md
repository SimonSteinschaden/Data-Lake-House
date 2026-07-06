# C# Entities

Die C# Entities liegen im Projekt:

`src/Enset.Domain/`

Die Domain ist nach Fachbereichen gegliedert:

- `Analytics`
- `Buildings`
- `Common`
- `Customers`
- `Data`
- `Documents`
- `Energy`
- `Geography`
- `Projects`

Zusätzlich enthält `src/Enset.Application/Imports/Models` importbezogene Modelle wie `ImportJob` und `RawDataObject`, die im Application-Layer liegen.

## Wichtige Domain-Entities

- `Customer`
- `Project`
- `Building`
- `District`
- `Municipality`
- `Region`
- `EnergySystem`
- `Meter`
- `MeterReading`
- `Document`
- `CalculationResult`
- `BenchmarkDataset`
- `DataSource`

## Bemerkungen

- `BaseEntity` definiert `Id`, `CreatedAt` und `UpdatedAt`.
- `MeterReading` ist das einzige Domain-Objekt ohne `BaseEntity`.
- `Meter` besitzt fachlich eindeutige `MeterNumber`.
- `ImportJob` und `DataSource` sind modelliert, aber nicht vollständig in der Persistenz registriert.

## Architekturhinweis

Die vollständige Implementierung der Entitäten liegt in der Codebase.
Dieses Dokument beschreibt Struktur, Zweck und aktuelle Implementierungsschwerpunkte.