# Importworkflow

## Aktiver Analysepfad

Der aktuelle Worker führt ausschließlich eine Importanalyse aus:

```text
Read
  -> Map
  -> Validate
  -> DuplicateCheck
  -> ImportDecisionEngine
  -> ImportReport
```

Es findet in diesem Pfad kein Schreibzugriff statt.

### 1. Read

- `IImportReader` ist der Application-Port für eine Importquelle.
- `ExcelImportReader` ist der Infrastructure-Adapter für einen Dateipfad.
- `ExcelWorkbookReader` öffnet die Arbeitsmappe mit ClosedXML und erzeugt ein `ImportWorkbook` mit `CustomerExcelRow` und `BuildingExcelRow`.
- ClosedXML ist nur in `Enset.Infrastructure` referenziert.

### 2. Map

- `IImportMapper` abstrahiert das Mapping.
- `CustomerImportMapper` überführt Customer-Zeilen in `CustomerImportDto`.
- Die DTOs werden im späteren `ImportReport` bereitgestellt.

### 3. Validate

- `IImportValidator` abstrahiert die Importvalidierung.
- `ExcelImportValidator` prüft aktuell leere und doppelte interne Customer-/Building-IDs sowie Customer-Referenzen von Gebäuden.
- Befunde werden als typisierte `ImportIssue`-Objekte im Report gesammelt.

### 4. DuplicateCheck

- `IDuplicationCheckService` ist der Port des Moduls.
- `DuplicationCheckService` prüft aktuell Customer-Dubletten.
- Interne `DuplicateCandidate<CustomerImportDto>` werden über einen Mapper in `ImportIssue` überführt.
- `DuplicateCandidate` verlässt das DuplicationCheck-Modul nicht.

### 5. ImportReport

`ImportCoordinator.RunAsync` liefert einen `ImportReport` mit:

- `ImportId` und `CreatedAt`;
- gemappten `Customers`;
- `Issues` mit stabilen `IssueId`-Werten;
- technischer `Decision` aus dem `ImportDecisionEngine`;
- Statistiken wie Customer-, Issue-, Error- und Warning-Count.

`ImportDecisionEngine` liefert `Abort`, sobald der Analyse-Report mindestens ein Error-/Critical-Issue enthält, sonst `Continue`. Das ist eine technische Analyseentscheidung und keine Benutzerfreigabe.

## Vorbereiteter Resolution- und Schreibpfad

```text
ImportReport
  -> ApplyResolutionService
  -> ImportWriteContext
  -> ImportWriteGate
  -> IImportWriter
```

### ApplyResolutionService

`IApplyResolutionService` und `ApplyResolutionService` sind implementiert. Der Service:

- ordnet Benutzerentscheidungen über `IssueId` zu;
- akzeptiert nur Issues mit `RequiresUserDecision`;
- validiert ResolutionAction und benutzerdefinierte Werte;
- setzt `ResolutionAction`, `CustomResolvedValue` und `IsResolved`;
- erzeugt einen `ImportWriteContext` mit Customers, Issues, aktueller technischer Entscheidung und Benutzerbestätigung.

Es gibt noch keine Persistenz, API oder UI für Reports und Entscheidungen.

### WriteGate

`ImportWriteGate` schreibt selbst keine Daten. Es verweigert die Freigabe, wenn:

- die Context-Decision `Abort` ist;
- die Benutzerbestätigung fehlt;
- mindestens ein entscheidungspflichtiges Issue ungelöst ist.

### Writer

- `IImportWriter` ist der Application-Port.
- `ExcelImportWriter` ist ein vorhandener Infrastructure-Adapter.
- `ExcelWorkbookWriter` führt die konkrete ClosedXML-Aktualisierung aus.
- Der aktive Worker instanziiert oder verwendet diese Writer derzeit nicht.
- Database- und Raw-Zone-Writer fehlen.

## Logging

Der Coordinator protokolliert über `IImportLogger`. Im Worker wird dafür `ConsoleImportLogger` verwendet. Eine strukturierte produktive Logging-Infrastruktur ist noch offen.

## Testwerkzeuge und Nebenpfade

- `DuplicationResolutionRunner` bleibt als auskommentiertes Entwickler-Testwerkzeug erhalten.
- `ConfiguredExcelImportReader` ist ein alternativer Worker-Adapter über `IExcelReader`.
- CSV-MeterReading-Reader, Factory, Lookup und Mapper existieren, gehören aber nicht zum aktiven Excel-Customer-Analysepfad.

## Noch offen

- persistente Ablage und Wiederabruf von `ImportReport`;
- API-/UI-gesteuerte Resolution;
- produktive Verdrahtung `ApplyResolutionService -> WriteGate -> Writer`;
- Audit Trail mit Benutzeridentität und Zeitstempel;
- transaktionaler Database-Writer und unveränderliche Raw-Kopie;
- durchgängige Building-, Meter- und MeterReading-Pipeline;
- automatisierte Tests für Coordinator, ResolutionService und WriteGate.
