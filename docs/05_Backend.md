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
  - Enthält Import- und Export-Infrastruktur.

- `src/Enset.Worker.Import/`
  - Entwickler-Test-Harness für CSV-/Excel-Importe und Validierungs-Experimente.

## Technischer Stack

- `TargetFramework`: `net10.0`
- `Microsoft.EntityFrameworkCore` 10.x
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x
- `ClosedXML` für Excel-Verarbeitung

## Wichtige Architekturentscheidungen

- `MeterReading` ist ein Zeitreihenobjekt und erbt **nicht** von `BaseEntity`.
- Der Primärschlüssel für `MeterReading` ist `MeterId + Timestamp`.
- `Meter` erbt von `BaseEntity` und verwendet `MeterNumber` als fachliche Identität.
- `MeterId` bleibt die technische interne GUID.
- Importdateien sollten `MeterNumber` verwenden, nicht `MeterId`.

## EnsetDbContext

- Befindet sich in `src/Enset.Infrastructure/DBContext.cs`.
- Konfiguriert:
  - `MeterReading` mit Composite Key `MeterId + Timestamp`.
  - Index auf `MeterReading.Timestamp`.
  - Unique Index auf `Meter.MeterNumber`.
  - Beziehung `Meter` -> `MeterReading` über `MeterId`.
- Definierte DbSets:
  - `Customers`
  - `Projects`
  - `Buildings`
  - `EnergySystems`
  - `Meters`
  - `MeterReadings`
  - `Documents`
  - `CalculationResults`
  - `BenchmarkDatasets`
- Auskommentiert, aber vorhanden:
  - `ImportJobs`
  - `DataSources`

## Import-Layer

- Abstraktionen in `Enset.Application/Imports/Abstractions`.
- DTOs in `Enset.Application/Imports/DTOs`.
- Modelle in `Enset.Application/Imports/Models`.
- Reader, Factory und Services in `Enset.Infrastructure/Imports`.

## Status und Ausblick

- `Enset.Domain`, `Enset.Application` und `Enset.Infrastructure` kompilieren.
- `Enset.Worker.Import` dient als Entwickler-Harness, nicht als produktiver Worker.
- API- und Frontend-Schicht sind noch nicht implementiert.
- Import-Service und persistente Import-Job-Verwaltung sind noch unvollständig.
