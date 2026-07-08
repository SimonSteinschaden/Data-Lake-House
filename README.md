# ENSET Data Lake House

Backend-Prototyp für das ENSET Data Lake House. Die verbindliche Zielarchitektur ist in der [Architecture Baseline v1.0](docs/Decisions/ARCHITECTURE_REVIEW_V1_0.md) festgehalten. Der aktuelle Implementierungsstand ist im [Architecture Review v1.2](docs/Decisions/ARCHITECTURE_REVIEW_V1_2.md) dokumentiert.

## Aktueller Stand

Implementiert sind:

- vier .NET-10-Projekte: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure` und `Enset.Worker`;
- ein Application-gesteuerter Analyseworkflow für Excel-Importe;
- die Pipeline `Read -> Map -> Validate -> DuplicateCheck -> ImportReport`;
- strukturierte `ImportIssue`- und `ImportReport`-Modelle;
- JSON-persistierte ImportReports mit Status, Source-Metadaten und Audit Trail;
- eine ASP.NET-Core-API für Analyze, Reportabruf, Resolutions und Commit;
- den gemeinsamen Commit-Pfad über `ApplyResolutionService`, `ImportCommitService`, `ImportWriteGate` und `IImportWriter`;
- Excel-Reader und Excel-Writer als Infrastructure-Adapter mit ClosedXML;
- einen dateibasierten Raw-Zone-Writer und einen sicheren DatabaseWriter-Platzhalter;
- automatisierte Architektur- und Workflowtests;
- EF-Core-Persistenz für das vorhandene Domainmodell mit PostgreSQL/Npgsql.

Der Worker führt derzeit ausschließlich die Importanalyse aus und gibt den Report in der Konsole aus. Er schreibt keine Importdaten.

Noch offen sind insbesondere React UI, fachliches Database-Mapping, datenbankgestützte Importhistorie, Background Jobs, Authentifizierung, OpenAPI und breitere Integrations-/End-to-End-Tests.

## Importablauf

Aktiv ausgeführt:

```text
ExcelImportReader
  -> ImportCoordinator
  -> Read -> Map -> Validate -> DuplicateCheck
  -> ImportReport
  -> Konsolenausgabe
```

Über API und Console-Test-Runner verfügbar:

```text
ImportReport
  -> ApplyResolutionService
  -> ImportWriteContext
  -> ImportWriteGate
  -> IImportWriter
  -> optional RawZoneWriter
```

## Technologie

- .NET 10
- Entity Framework Core 10
- Npgsql / PostgreSQL
- ClosedXML ausschließlich in `Enset.Infrastructure`

## Projektstruktur

- `src/Enset.Domain/`: Domain-Entities und fachliche Basistypen
- `src/Enset.Application/`: Import-Use-Case, Ports, DTOs, Validierung, DuplicationCheck, Entscheidungen und WriteGate
- `src/Enset.Infrastructure/`: EF Core, PostgreSQL/Npgsql sowie konkrete Datei-Adapter
- `src/Enset.Worker/`: Composition Root und Konsolen-Testpfad
- `src/Enset.Api/`: REST API und Composition Root für Phase 2
- `tests/Enset.Import.Tests/`: Architektur- und Workflowtests
- `docs/`: IST-Dokumentation, Baseline und Roadmap

## Build

```powershell
dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
dotnet build src/Enset.Api/Enset.Api.csproj --no-restore
dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj --no-restore
```

Letzter dokumentierter Stand: beide Builds erfolgreich; 7 von 7 Tests bestanden.
