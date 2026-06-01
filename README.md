# Data-Lake-House
Data Lake House für ENSET Universe

## Aktueller Stand
- Backend in drei Projekten: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`
- Implementiert: Domain-Entities, Import-Abstraktionen, EF Core Persistenz mit PostgreSQL/TimescaleDB
- Noch offen: API/Web-UI, externe Connectoren, ETL-Worker und Data Marketplace

## Technologie
- .NET 10
- Entity Framework Core 10
- Npgsql / PostgreSQL
- TimescaleDB-kompatibles Zeitreihenmodell

## Struktur
- `src/Enset.Domain/` enthält das reine Domain-Modell
- `src/Enset.Application/` enthält Import-DTOs, Abstraktionen und Prozessmodelle
- `src/Enset.Infrastructure/` enthält die Persistenz und konkrete Import-Implementierungen
