# Backend-Architektur

## Aktuelle Projektstruktur

Das Backend ist sauber nach Clean Architecture getrennt:

- src/Enset.Domain/
  - Enthält ausschließlich Domain-Entities, Enums und reine Business-Logik.
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

- src/Enset.Application/
  - Referenziert Enset.Domain.
  - Enthält Import-DTOs, Abstraktionen, Enums und Prozess-/Importmodelle.
  - Packages:
    - Imports/DTOs
    - Imports/Abstractions
    - Imports/Enums
    - Imports/Models

- src/Enset.Infrastructure/
  - Referenziert Enset.Domain und Enset.Application.
  - Enthält EF Core EnsetDbContext, TimescaleDB-konforme Persistenz, Reader-Implementierungen, Mapper-Implementierungen und konkrete Services.
  - Enthält auch Import-spezifische Infrastrukturklassen.

## Wichtige Architekturentscheidungen

- MeterReading ist ein Timeseries-Entity und erbt **nicht** von BaseEntity.
- MeterReading verwendet den Composite Key MeterId + Timestamp.
- Meter erbt von BaseEntity und enthält MeterNumber als fachliche Identität.
- MeterId bleibt technische interne GUID.
- MeterNumber ist die fachliche/externe Identität und darf von Import-Dateien verwendet werden.
- Excel/CSV/XML-Schichten dürfen keine interne MeterId verlangen.

## EnsetDbContext

- Liegt in src/Enset.Infrastructure/DBContext.cs.
- Konfiguriert:
  - MeterReading mit Composite Key MeterId + Timestamp.
  - Meter mit Unique Index auf MeterNumber.
  - Beziehung Meter -> MeterReading.
- DbSet<ImportJob> und DbSet<DataSource> wurden aus dem DbContext entfernt, weil diese Modelle nicht als Domain-Persistenzobjekte in der aktuellen Architektur behandelt werden.

## Import-Layer

- Interfaces in Enset.Application/Imports/Abstractions:
  - IMeterReadingReader
  - IMeterReadingReaderFactory
  - IMeterLookupService
  - IMeterReadingMapper
- DTOs in Enset.Application/Imports/DTOs:
  - MeterImportDto
  - MeterReadingImportDto
- Implementierungen in Enset.Infrastructure/Imports:
  - CsvMeterReadingReader
  - MeterReadingReaderFactory
  - MeterLookupService
  - MeterReadingMapper

## Build-Status

- dotnet restore und dotnet build sind erfolgreich für alle drei Projekte.
- Es gibt keine Compile-Fehler mehr in Enset.Domain, Enset.Application oder Enset.Infrastructure.
- Es bleiben nur Paket-Warnungen zur externen Bibliothek System.Security.Cryptography.Xml, die nicht Teil der Architekturänderung sind.
