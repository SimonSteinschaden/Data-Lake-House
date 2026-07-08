# Importworkflow

## Analyse

```text
Excel
  -> IImportAnalysisService
  -> ImportCoordinator
  -> Read -> Map -> Validate -> DuplicateCheck
  -> ImportReport
  -> IImportReportRepository
```

`ExcelImportAnalysisService` staged die hochgeladene Datei, berechnet SHA-256, führt den strikt analyse-only gehaltenen Coordinator aus und speichert den Report. Der Coordinator kennt weder Gate noch Writer.

### Komponenten

- `IImportReader` / `ExcelImportReader`: Application-Port und Datei-Adapter
- `ExcelWorkbookReader`: ClosedXML-Parsen in Infrastructure
- `IImportMapper` / `CustomerImportMapper`: Mapping auf CustomerImportDto
- `IImportValidator` / `ExcelImportValidator`: strukturierte Validierungs-Issues
- `IDuplicationCheckService` / `DuplicationCheckService`: Customer-Dubletten und Issue-Mapping
- `ImportDecisionEngine`: technische Continue-/Abort-Entscheidung

### Persistenter ImportReport

Der Report speichert:

- ImportId und ImportStatus;
- Customers und ImportIssues;
- SourceFile-Metadaten einschließlich SHA-256 und internem Stagingpfad;
- CreatedAt und UpdatedAt;
- Decision, Statistiken und Audit Trail.

`JsonImportReportRepository` ist die aktuelle austauschbare dateibasierte Implementierung von `IImportReportRepository`.

## Resolution

```text
persistierter ImportReport
  -> ApplyResolutionService
  -> aktualisierter ImportReport + Audit Trail
  -> IImportReportRepository
```

Der Service ordnet Entscheidungen über IssueId zu, erlaubt Änderungen oder Zurücksetzen vor dem Commit, validiert benutzerdefinierte Werte und protokolliert Benutzer, Zeitpunkt sowie Vorher-/Nachher-Zustand. Er schreibt keine Nutzdaten und ruft keinen Writer auf.

## Commit

```text
ImportCommitRequest
  -> ImportCommitService
  -> ImportWriteContext
  -> ImportWriteGate
  -> IImportWriter
  -> optional IRawZoneWriter
```

`ImportWriteContext` enthält ImportId, Report, Zielmodus, Zielwriter, UserId, Timestamp, TargetLocation und Raw-Zone-Option.

Das Gate blockiert, wenn:

- Report oder passende ImportId fehlen;
- der Benutzerkontext fehlt;
- der Status nicht `ReadyToCommit` ist;
- die Decision `Abort` ist;
- entscheidungspflichtige Issues offen sind.

`ImportWriteGateResult` liefert alle Gate-Fehler. Nur `ImportCommitService` löst nach erfolgreichem Gate einen Writer auf.

### Writer

- `ExcelImportWriter`: kopiert die gestagte Arbeitsmappe ans Ziel und aktualisiert Customers über `ExcelWorkbookWriter`.
- `DatabaseImportWriter`: registrierbarer sicherer Platzhalter; verweigert Änderungen, bis das fachliche Domain-Mapping implementiert ist.
- `FileSystemRawZoneWriter`: archiviert die Originaldatei optional nach erfolgreichem Ziel-Write eindeutig unter der ImportId.

## Gemeinsame Aufrufer

- REST API: Analyze, GET, Resolutions und Commit
- `DuplicationResolutionRunner`: Console-/Entwickler-Testpfad über dieselben Application-Services
- zukünftige React UI: soll ausschließlich die REST API verwenden

Es existiert keine parallele Console-Resolution- oder Writerlogik.

## Noch offen

- React Import Wizard;
- fachliches und transaktionales Database-Mapping;
- datenbankgestützte Report-/Import-History-Persistenz mit Concurrency Control;
- Authentifizierung statt übermittelter UserId;
- Building-, Meter- und MeterReading-End-to-End-Pipeline;
- breitere Excel-, API-, Sicherheits- und End-to-End-Tests.
