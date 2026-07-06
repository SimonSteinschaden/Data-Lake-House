# Architekturüberblick

Dieses Dokument beschreibt ausschließlich die aktuell implementierte Architektur. Zielvorgaben bleiben in `Decisions/ARCHITECTURE_REVIEW_V1_0.md` verbindlich.

## Projekt- und Abhängigkeitsstruktur

```text
Enset.Domain
    ^
Enset.Application
    ^
Enset.Infrastructure
    ^
Enset.Worker (Composition Root)
```

Der Worker referenziert für die Komposition alle drei Bibliotheken. Die fachliche Importorchestrierung liegt dagegen in `Enset.Application`.

### Domain

- reine Domain-Entities und Enums;
- keine Projekt- oder Package-Abhängigkeiten;
- keine EF-Core-, ClosedXML- oder Dateisystemlogik;
- zentrale Modelle: `Customer`, `Project`, `Building`, `Meter`, `MeterReading`, `CalculationResult` und `BenchmarkDataset`.

### Application

- referenziert ausschließlich `Enset.Domain`;
- enthält Ports wie `IImportReader`, `IImportWriter`, `IImportCoordinator`, `IImportWriteGate` und `IImportLogger`;
- enthält den Analyse-Use-Case `ImportCoordinator`;
- enthält Mapping, Validierung, DuplicationCheck, Reports, Entscheidungen, Resolution und WriteGate;
- kennt weder ClosedXML noch konkrete Excel-Arbeitsmappen.

### Infrastructure

- referenziert Domain und Application;
- enthält `EnsetDbContext`, EF Core und Npgsql;
- enthält die konkreten Excel-Adapter und ausschließlich hier die ClosedXML-Abhängigkeit;
- enthält weitere CSV-, Mapping- und Lookup-Bausteine, die nicht Teil des aktiven Excel-Analysepfads sind.

### Worker

- Konsolenanwendung und Composition Root;
- erzeugt Reader, Mapper, Validator, DuplicationCheck, Logger und Coordinator;
- `ImportRunner` delegiert an `IImportCoordinator`;
- gibt den `ImportReport` aus;
- schreibt im aktuellen Programmpfad keine Importdaten;
- verwendet noch einen hart codierten lokalen Entwicklungspfad und ist kein produktiver Hosted Worker.

## Aktive Importarchitektur

```text
IImportReader / ExcelImportReader
    -> IImportMapper / CustomerImportMapper
    -> IImportValidator / ExcelImportValidator
    -> IDuplicationCheckService / DuplicationCheckService
    -> ImportDecisionEngine
    -> ImportReport
```

`ImportCoordinator` orchestriert diese Komponenten. Er enthält weder `IImportWriteGate` noch `IImportWriter` und führt keine Benutzerentscheidung oder Persistenz aus.

## Vorbereiteter Freigabe- und Schreibpfad

```text
ImportReport
    -> IApplyResolutionService / ApplyResolutionService
    -> ImportWriteContext
    -> IImportWriteGate / ImportWriteGate
    -> IImportWriter
    -> z. B. ExcelImportWriter
```

Die einzelnen Bausteine existieren. Dieser zweite Teil ist noch nicht über API, UI oder einen produktiven Worker-Endpunkt verdrahtet.

## Durchgesetzte Architekturentscheidungen

- Der Coordinator orchestriert ausschließlich die Analyse.
- `ImportIssue` ist das zentrale Workflowmodell für Validierungs- und Dublettenprobleme.
- `DuplicateCandidate<T>` bleibt intern im DuplicationCheck und wird vor Verlassen des Moduls in `ImportIssue` überführt.
- Excel und ClosedXML bleiben in Infrastructure.
- Reader und Writer folgen derselben Port-/Adapterstruktur: Application-Port, Infrastructure-Adapter, technische Workbook-Komponente.
- Logging im Use Case erfolgt über `IImportLogger`; der Worker stellt `ConsoleImportLogger` bereit.
- `ImportWriteGate` bewertet ausschließlich `ImportWriteContext`, Bestätigung und Issue-Zustände.

## Noch nicht implementierte Architekturteile

- REST API und Dependency-Injection-Host
- React Import Wizard
- persistente Speicherung des `ImportReport`
- produktiver End-to-End-Resolution-/Write-Pfad
- `DatabaseImportWriter` und `RawZoneWriter`
- Background Jobs und Import History
- Authentifizierung/Autorisierung
- Data-Product-Ports und produktive Zonenpipeline
- automatisierte Unit-, Integrations- und Architekturtests
