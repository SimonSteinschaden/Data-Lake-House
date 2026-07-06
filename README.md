# ENSET Data Lake House

Backend-Prototyp für das ENSET Data Lake House. Die verbindliche Zielarchitektur ist in der [Architecture Baseline v1.0](docs/Decisions/ARCHITECTURE_REVIEW_V1_0.md) festgehalten. Der aktuelle Implementierungsstand ist im [Architecture Review v1.1](docs/Decisions/ARCHITECTURE_REVIEW_V1_1.md) dokumentiert.

## Aktueller Stand

Implementiert sind:

- vier .NET-10-Projekte: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure` und `Enset.Worker`;
- ein Application-gesteuerter Analyseworkflow für Excel-Importe;
- die Pipeline `Read -> Map -> Validate -> DuplicateCheck -> ImportReport`;
- strukturierte `ImportIssue`- und `ImportReport`-Modelle;
- `ApplyResolutionService`, `ImportWriteContext` und `ImportWriteGate` als Bausteine des nachgelagerten Freigabepfads;
- Excel-Reader und Excel-Writer als Infrastructure-Adapter mit ClosedXML;
- EF-Core-Persistenz für das vorhandene Domainmodell mit PostgreSQL/Npgsql.

Der Worker führt derzeit ausschließlich die Importanalyse aus und gibt den Report in der Konsole aus. Er schreibt keine Importdaten.

Noch offen sind insbesondere REST API, React UI, persistente ImportReports, produktiver Resolution-/Write-Ablauf, Database- und Raw-Zone-Writer, Background Jobs, Authentifizierung, Importhistorie und automatisierte Tests.

## Importablauf

Aktiv ausgeführt:

```text
ExcelImportReader
  -> ImportCoordinator
  -> Read -> Map -> Validate -> DuplicateCheck
  -> ImportReport
  -> Konsolenausgabe
```

Vorbereitet, aber noch nicht im Worker verdrahtet:

```text
ImportReport
  -> ApplyResolutionService
  -> ImportWriteContext
  -> ImportWriteGate
  -> IImportWriter
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
- `docs/`: IST-Dokumentation, Baseline und Roadmap

## Build

```powershell
dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
```

Letzter dokumentierter Stand: erfolgreich, 0 Warnungen, 0 Fehler.
