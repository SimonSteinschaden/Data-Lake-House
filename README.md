# ENSET Data Lake House

ENSET ist ein .NET-/React-System für Import, Qualitätssicherung, relationale Energiedaten und versionierte Data Products.

## Architecture Freeze 1.0 RC

- [Verbindliche Architektur-Baseline](docs/ARCHITECTURE_BASELINE_V1_0_RC.md)
- [Architecture Review](docs/Decisions/ARCHITECTURE_REVIEW_V1_0_RC.md)
- [Offene Implementierungsroadmap](docs/IMPLEMENTATION_ROADMAP_V1_0_RC.md)
- [Data Product Engine](docs/DATA_PRODUCT_ENGINE_V1_0_RC.md)
- [PlantUML-Diagramme](docs/UML/)

## Projekte

- `Enset.Domain`: Domainentities und Enums
- `Enset.Application`: Use Cases und Ports
- `Enset.Infrastructure`: EF Core, PostgreSQL-, Import- und Filesystemadapter
- `Enset.Api`: versionierte REST API
- `Enset.Worker`: Import-Console-Host im Entwicklungsstand
- `Enset.Web`: React/Vite-Anwendung
- `Enset.Import.Tests`: xUnit-Architektur-, Pipeline- und Controller-Tests

## Implementierter Funktionsumfang

- Importanalyse `Read → Map → Validate → DuplicateCheck → ImportReport`
- API-gestützte Resolution- und Commit-Orchestrierung mit Write Gate
- relationale ImportReport-, Issue- und Audit-Persistenz
- Filesystem Raw Zone und Excel-Infrastructure-Adapter
- PostgreSQL-/EF-Core-Persistenz für das aktuelle Domainmodell
- Data Product Engine für `METER_CONSUMPTION_SUMMARY` und `BUILDING_ENERGY_PROFILE`
- versionierte Import- und Data-Product-REST-API
- React Import Wizard und Data-Product-Dashboard

Der relationale `DatabaseImportWriter` und ein produktiver Workerbetrieb sind noch offen. Details stehen in der RC-Roadmap.

## Technologie

- .NET 10, ASP.NET Core und Entity Framework Core 10
- Npgsql/PostgreSQL
- ClosedXML in Infrastructure
- React 19, TypeScript und Vite

## Build

```powershell
dotnet build src/Enset.Api/Enset.Api.csproj -c Release --no-restore
dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj -c Release --no-restore
cd src/Enset.Web
npm run lint
npm run build
```

## Nachgewiesener Buildstand

Zum Freeze-Zeitpunkt kompilieren API und Frontend ohne Warnungen; 19 xUnit-Tests sind erfolgreich. Ein vollständiger Lauf gegen eine reale PostgreSQL-Instanz ist als P0 offen.
