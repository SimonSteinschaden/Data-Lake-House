# Backend-Architektur

## Aktuelle Projektstruktur

Das Backend ist sauber nach Clean Architecture getrennt:

- `src/Enset.Domain/`
  - Enthält Domain-Entities, Enums und das Basismodell.
  - Keine Abhängigkeit auf EF Core, Infrastructure oder Application.
  - Packages:
    - Common
    - Customers
    - Projects
    - Buildings
    - Energy
    - Documents
    - Analytics
    - Geography
    - Data

- `src/Enset.Application/`
  - Referenziert `Enset.Domain`.
  - Enthält Import-DTOs, Abstraktionen, Enums und Prozessmodelle.
  - Packages:
    - Imports/DTOs
    - Imports/Abstractions
    - Imports/Enums
    - Imports/Models

- `src/Enset.Infrastructure/`
  - Referenziert `Enset.Domain` und `Enset.Application`.
  - Enthält EF Core `EnsetDbContext`, TimescaleDB-kompatible Persistenz, Reader-Implementierungen, Mapper-Implementierungen und konkrete Services.
  - Enthält auch Import-spezifische Infrastrukturklassen.

## Technischer Stack

- `TargetFramework`: `net10.0`
- `Microsoft.EntityFrameworkCore` 10.0.4
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1
- `Microsoft.EntityFrameworkCore.Design` 10.0.1

## Wichtige Architekturentscheidungen

- `MeterReading` ist ein Zeitreihen-Entity und erbt **nicht** von `BaseEntity`.
- `MeterReading` verwendet einen Composite Key aus `MeterId` und `Timestamp`.
- `Meter` erbt von `BaseEntity` und nutzt `MeterNumber` als fachliche Identität.
- `MeterId` bleibt die technische interne GUID.
- `MeterNumber` ist die fachliche/externe Identität und darf von Import-Dateien verwendet werden.
- Importquellen sollten `MeterNumber` verwenden, nicht `MeterId`.

## EnsetDbContext

- Befindet sich in `src/Enset.Infrastructure/DBContext.cs`.
- Konfiguriert:
  - `MeterReading` mit Composite Key `MeterId + Timestamp`.
  - Index auf `MeterReading.Timestamp`.
  - Einzigartigen Index auf `Meter.MeterNumber`.
  - Beziehung `Meter` -> `MeterReading` über `MeterId`.
- Enthält `DbSet` für:
  - `Customers`, `Projects`, `Buildings`
  - `EnergySystems`, `Meters`, `MeterReadings`
  - `Documents`, `CalculationResults`, `BenchmarkDatasets`
- `DbSet<ImportJob>` und `DbSet<DataSource>` sind aktuell aus dem DbContext auskommentiert.

## Import-Layer

- Abstraktionen in `Enset.Application/Imports/Abstractions`:
  - `IMeterReadingReader`
  - `IMeterReadingReaderFactory`
  - `IMeterLookupService`
  - `IMeterReadingMapper`
- DTOs in `Enset.Application/Imports/DTOs`:
  - `MeterImportDto`
  - `MeterReadingImportDto`
- Modelle in `Enset.Application/Imports/Models`:
  - `ImportJob`
  - `RawDataObject`
- Implementierungen in `Enset.Infrastructure/Imports`:
  - `CsvMeterReadingReader`
  - `MeterReadingReaderFactory`
  - `MeterLookupService`
  - `MeterReadingMapper`

## Status und Ausblick

- `dotnet restore` und `dotnet build` laufen aktuell für alle drei Projekte.
- `Enset.Domain`, `Enset.Application` und `Enset.Infrastructure` sind kompilierbar.
- Es ist derzeit keine API-Implementierung vorhanden; die Webschicht ist noch offen.
