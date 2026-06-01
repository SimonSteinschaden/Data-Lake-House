# Datenmodell

## Kern-Entities

- `Customer`
  - Enthält Kundenstammdaten.
  - Beziehung zu `Project`.

- `Project`
  - Gehört zu einem `Customer`.
  - Enthält `Buildings` und `Documents`.

- `Building`
  - Gehört zu einem `Project`.
  - Gehört zu einem `District`.
  - Enthält `EnergySystems` und `Meters`.

- `Meter`
  - Gehört zu einem `Building`.
  - Hat `MeterNumber` als fachliche Identität.
  - Besitzt das Attribut `Unit` (Standard: `kWh`).
  - Eine `Meter`-Entität kann viele `MeterReading`-Einträge haben.

- `MeterReading`
  - Zeitreihendatenpunkt für einen `Meter`.
  - Verwendet den Composite Key `MeterId + Timestamp`.
  - Enthält `Value`, `Unit`, `QualityFlag` und optional `CustomerId`, `BuildingId`, `SourceImportJobId`.

- `Document`
  - Dokumente zum Projekt.

- `CalculationResult`
  - Ergebnis einer Analyse oder Berechnung.

- `BenchmarkDataset`
  - Datensatz für Benchmark-Vergleiche.

## Basisobjekt

- `BaseEntity`
  - `Id` (GUID)
  - `CreatedAt` (UTC)
  - `UpdatedAt`

## Datenbankmodell

- `MeterReading` ist kein `BaseEntity`; es ist ein reines Zeitreihenmodell.
- `Meter` besitzt einen eindeutigen, fachlichen Index auf `MeterNumber`.
- `MeterReading` ist über `MeterId` mit `Meter` verknüpft.
- Import-Jobs und DataSources sind aktuell nicht als `DbSet` im `EnsetDbContext` eingebunden.