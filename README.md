# Data-Lake-House
Data Lake House für ENSET Universe

## Aktueller Stand
- Backend-Schichten: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`
- Entwickler-Test-Harness: `Enset.Worker.Import`
- Implementiert:
  - Domainmodell mit Kunden, Projekten, Gebäuden, Energieanlagen, Zählern und Zeitreihen
  - Import-Abstraktionen, DTOs, Reader-Factory und einfache CSV-/Excel-Leser
  - EF Core Persistenz mit PostgreSQL/TimescaleDB-kompatiblen `MeterReadings`
  - Excel-Export-Utilities und grundsätzliche Import-Validierungs-Tools
- Offen:
  - produktive ASP.NET Core API / HTTP-Endpunkte
  - Web- oder Desktop-Frontend
  - produktiver Worker / orchestrierter ETL-Workflow
  - vollständige Import-/Mapping-/Persistenz-Pipeline
  - Data Marketplace

## Technologie
- .NET 10
- Entity Framework Core 10
- Npgsql / PostgreSQL
- TimescaleDB-kompatibles Zeitreihenmodell
- ClosedXML für Excel-Integration

## Struktur
- `src/Enset.Domain/` enthält das reine Domain-Modell
- `src/Enset.Application/` enthält Import-DTOs, Abstraktionen und Prozessmodelle
- `src/Enset.Infrastructure/` enthält Persistenz, Import-/Export-Infrastruktur und konkrete Services
- `src/Enset.Worker.Import/` enthält einen Entwickler-Test-Harness für CSV-/Excel-Importe
