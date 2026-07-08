# ENSET Data Lake House – Architecture Review v1.1

**Prüfdatum:** 2026-07-06  
**Prüfgegenstand:** aktueller Implementierungsstand des Repository  
**Verbindliche Referenz:** `ARCHITECTURE_REVIEW_V1_0.md` / ENSET Architecture Baseline v1.0  
**Charakter dieses Dokuments:** reiner IST-Review, keine neue Zielarchitektur

## Einordnung

Seit dem Review v1.0 wurde der Importworkflow deutlich näher an die Baseline geführt. Die Importanalyse wird in Application orchestriert und ist vom Resolution-/Schreibpfad getrennt. Phase 2 ergänzt persistente Reports, Audit Trail, REST-Endpunkte, einen zentralen Commit-Service, Raw-Zone-Archivierung und automatisierte Kernprüfungen. Die Baseline v1.0 ist trotzdem noch nicht vollständig umgesetzt: React UI, fachliches Database-Mapping, produktive Storage-Backends, Authentifizierung und Betrieb fehlen.

## A) Aktuell implementierter Stand

### ImportRunner

`ImportRunner` im Namespace `Enset.Worker` ist ein dünner Konsolen-Runner. Er hängt ausschließlich von `IImportCoordinator` ab, delegiert `RunAsync` und gibt dessen `Task<ImportReport>` zurück. Er enthält keine fachliche Analyse-, Resolution- oder Writerlogik.

### ImportCoordinator

`Enset.Application.Imports.Coordination.ImportCoordinator` ist der Application-Use-Case für die Importanalyse. Er orchestriert Reader, Mapper, Validator, DuplicationCheck und Logger. Er aggregiert Issues, bestimmt die technische Importentscheidung und liefert einen `ImportReport`.

Der Coordinator enthält weder `IImportWriteGate` noch `IImportWriter`. Er trifft keine Benutzerentscheidung und schreibt keine Daten.

### ImportReader und IImportReader

`IImportReader` ist der Application-Port. Seine `Read`-Methode liefert ein formatneutrales `ImportWorkbook`. `ExcelImportReader` implementiert diesen Port in Infrastructure, öffnet den konfigurierten Dateipfad als Stream und delegiert das Parsen an `IExcelWorkbookReader`.

### ExcelWorkbookReader

`ExcelWorkbookReader` ist die technische ClosedXML-Komponente. Sie liest die Tabellen `Customers` und `Buildings` und erzeugt `CustomerExcelRow`- und `BuildingExcelRow`-Modelle. Die ClosedXML-Abhängigkeit bleibt vollständig in Infrastructure.

### ImportMapper

`IImportMapper` ist der Application-Port für das Mapping. `CustomerImportMapper` mappt Customer-Zeilen in `CustomerImportDto`. Der aktive Workflow verarbeitet derzeit Customers; eine gleichwertige vollständige Pipeline für Buildings, Meter und MeterReadings ist noch nicht integriert.

### ImportValidator

`IImportValidator` ist der Application-Port. `ExcelImportValidator` prüft aktuell:

- fehlende Customer- und Building-Datensätze;
- leere interne Customer-/Building-IDs;
- doppelte interne IDs;
- fehlende oder unbekannte Customer-Referenzen von Buildings.

Alle Befunde werden als `ImportIssue` erfasst. Mehrere spezialisierte Validator-Dateien sind weiterhin leere Platzhalter.

### DuplicationCheckService

`IDuplicationCheckService` kapselt den DuplicationCheck. Die aktuelle Implementierung prüft Customer-Dubletten anhand interner Identity-/Similarity-Hilfen. `DuplicateCandidate<T>` bleibt innerhalb des Moduls und wird durch `CustomerDuplicateIssueMapper` in ein `ImportIssue` überführt.

### ImportIssue

`ImportIssue` ist das zentrale Workflowmodell. Es enthält unter anderem:

- stabile `IssueId` und optionale `EntityId`;
- Typ, Severity und Meldung;
- optionalen SimilarityScore und Feld-/Vergleichswerte;
- `RequiresUserDecision`;
- `ResolutionAction`, `CustomResolvedValue` und `IsResolved`.

Damit transportiert der Workflow Validierungs- und Dublettenprobleme einheitlich bis zur späteren Benutzerentscheidung.

### ImportReport

`ImportReport` ist das Ergebnis der Analyse. Es enthält:

- `ImportId` und UTC-`CreatedAt`;
- gemappte Customers;
- alle `ImportIssue`-Objekte;
- eine technische `ImportDecision`;
- BuildingCount und abgeleitete Customer-, Issue-, Error- und Warning-Counts;
- gefilterte Error-, Warning-, Information- und Critical-Listen.

Der Report wird über `IImportReportRepository` gespeichert. Die aktuelle JSON-Dateiimplementierung ist für API-/UI-Abruf geeignet, ersetzt aber noch kein produktives Datenbankrepository mit Concurrency Control.

### ImportDecisionEngine

`ImportDecisionEngine` bewertet den Analyse-Report. Bei mindestens einem Error oder Critical Issue erzeugt er `Abort`, sonst `Continue`, jeweils mit einer Begründung. Diese Entscheidung ist keine Benutzerfreigabe.

### ApplyResolutionService

`IApplyResolutionService` und `ApplyResolutionService` sind implementiert. Der Service ordnet Benutzerentscheidungen über `IssueId` zu, verhindert doppelte Resolutionen innerhalb eines Requests, erlaubt spätere Änderungen, validiert Action und Custom Value und aktualisiert Status sowie Decision. Jede Änderung wird mit Benutzer, Zeitpunkt und Vorher-/Nachher-Werten auditierbar gespeichert. Der Service erzeugt keinen WriteContext und schreibt keine Nutzdaten.

### ImportWriteContext

`ImportWriteContext` bündelt die für eine Schreibfreigabe relevanten Informationen:

- ImportId und vollständigen ImportReport;
- Zielmodus und Zielwriter (`Excel` oder `Database`);
- UserId und Timestamp;
- optionale TargetLocation und Raw-Zone-Option;
- abgeleitete Issues und Customer-DTOs.

### ImportWriteGate

`IImportWriteGate` abstrahiert die Freigabe. `ImportWriteGate` erlaubt Schreiben nur bei vorhandenem passenden Report, User-Kontext, Status `ReadyToCommit`, einer Decision ungleich `Abort` und ohne offene entscheidungspflichtige Issues. Es liefert ein strukturiertes Gate-Ergebnis, bewertet ausschließlich den `ImportWriteContext` und schreibt selbst nichts.

### IImportWriter und ExcelImportWriter

`IImportWriter` ist der Application-Port für einen freigegebenen Schreibvorgang. `ExcelImportWriter` ist der vorhandene Infrastructure-Adapter und delegiert die technische Aktualisierung an `IExcelWorkbookWriter`. Im aktiven Workerpfad wird kein Writer aufgerufen.

### ExcelWorkbookWriter

`ExcelWorkbookWriter` verwendet ClosedXML, öffnet eine konfigurierte Arbeitsmappe und aktualisiert passende Customer-Zeilen anhand der externen Customer-ID. `ExcelImportWriter` wird ausschließlich vom zentralen Commit-Service nach erfolgreichem Gate aufgerufen.

### ImportLogger

`IImportLogger` ist der Application-Port mit Info-, Warning- und Error-Operationen. `ConsoleImportLogger` ist der Worker-Adapter und schreibt in Standardausgabe bzw. Standardfehler. Strukturiertes produktives Logging und Monitoring sind noch offen.

## B) Aktuelle Importpipeline

### Aktiv ausgeführter Teil

```text
Read
  ↓
Map
  ↓
Validate
  ↓
DuplicateCheck
  ↓
ImportReport
```

Der aktuelle `Program.cs` erstellt `ExcelImportReader`, Mapper, Validator, DuplicationCheck, Logger und Coordinator, führt den Runner aus und gibt ausschließlich Report und Issues aus. Es wird keine Ausgabedatei erzeugt und kein Writer aufgerufen.

### Vorbereiteter zweiter Teil

```text
ImportReport
  ↓
ApplyResolutionService
  ↓
ImportWriteContext
  ↓
WriteGate
  ↓
Writer
```

Der zweite Teil ist über `ImportCommitService` zentral verdrahtet. REST API und `DuplicationResolutionRunner` nutzen denselben Resolution-/Commit-Pfad. Das WriteGate wird erst nach geladenem Report und angewendeten Benutzerentscheidungen aufgerufen; nur der Commit-Service löst anschließend einen Writer auf.

## C) Projektstruktur

| Layer | Aktuelle Verantwortung | Technische Grenze |
|---|---|---|
| Application | Import-Use-Case, Ports, DTOs, Validierung, DuplicationCheck, Reports, Decisions, Resolution und WriteGate | referenziert nur Domain; kein ClosedXML |
| Infrastructure | EF Core/Npgsql, Excel-/CSV-Adapter, Workbook-Zugriff, Lookup und Migrationen | referenziert Application und Domain; enthält konkrete I/O-Packages |
| Worker | Composition Root, Entwicklungskonfiguration, Runner, Konsolenlogger und Reportausgabe | referenziert die drei Bibliotheken; kein produktiver Host |
| Domain | Entities, Enums und fachliche Basistypen | keine Projekt- oder Package-Referenzen |

Vorhanden sind zusätzlich `Enset.Api` und `tests/Enset.Import.Tests`. Nicht vorhanden sind ein UI-Projekt und eine Solution-Datei.

## D) Architekturentscheidungen

1. **Coordinator enthält ausschließlich Orchestrierung.** Er analysiert und liefert einen Report; Resolution und Schreiben sind ausgeschlossen.
2. **ImportIssue ist das zentrale Workflowmodell.** Validierungs- und Dublettenbefunde werden einheitlich als Issues transportiert.
3. **DuplicateCandidate bleibt intern.** Kandidaten werden vor Verlassen des DuplicationCheck-Moduls in Issues übersetzt.
4. **Excel ist ausschließlich Infrastructure.** Dateizugriff und ClosedXML befinden sich nicht in Application oder Domain.
5. **Application kennt keine ClosedXML-Abhängigkeit.** Das Application-Projekt referenziert nur Domain.
6. **Reader und Writer besitzen eine symmetrische Adapterstruktur.** Application definiert Port und Context/Modelle; Infrastructure implementiert den formatbezogenen Adapter und die Workbook-Technik.
7. **Logging erfolgt über IImportLogger.** Der Use Case ist nicht an die Konsole oder ein konkretes Logging-Framework gebunden.
8. **WriteGate arbeitet ausschließlich auf ImportWriteContext.** Es führt keine Resolution, Persistenz oder Benutzerinteraktion aus.

## E) Implementierungsstand

Legende: ✅ implementiert, 🟡 teilweise implementiert, ⬜ offen

| Modul | Status | Aktueller Befund |
|---|:---:|---|
| ImportReader | ✅ | Port und aktiver Excel-Adapter vorhanden |
| ImportWriter | 🟡 | Excel-Adapter über Commit verdrahtet; Database-Mapping noch sicher blockiert |
| Logging | 🟡 | Port und Console-Adapter vorhanden; strukturiertes Logging fehlt |
| Validation | 🟡 | aktive Excel-Customer-/Building-Prüfungen; weitere Validatoren und Tests fehlen |
| DuplicateCheck | 🟡 | Customer-Erkennung und Issue-Mapping vorhanden; weitere Entitäten/Audit fehlen |
| DecisionEngine | ✅ | technische Continue-/Abort-Entscheidung vorhanden |
| ImportReport | ✅ | JSON-Persistenz, Status, Source-Metadaten und Audit Trail vorhanden |
| ApplyResolutionService | ✅ | wiederholbare, auditierte Entscheidungen über API/Console-Pfad integriert |
| ImportWriteGate | ✅ | Context-basierte Freigabelogik vorhanden |
| REST API | 🟡 | Analyze, GET, Resolutions und Commit vorhanden; Auth/OpenAPI fehlen |
| React UI | ⬜ | kein Frontend-Projekt |
| DatabaseWriter | 🟡 | Writer registrierbar, verweigert sicher bis fachliches Mapping existiert |
| RawZoneWriter | ✅ | dateibasierte eindeutige Archivierung nach ImportId vorhanden |
| Authentication | ⬜ | nicht implementiert |
| ImportHistory | ⬜ | nicht implementiert |
| Audit | 🟡 | Report-Audit Trail vorhanden; unveränderliche DB-Historie fehlt |
| Background Jobs | ⬜ | kein Hosted Worker, Queueing oder Scheduling |
| Automatisierte Tests | 🟡 | 7 Kern-/Architekturtests vorhanden; breite Integration/E2E fehlt |
| EF-Core-Domainpersistenz | 🟡 | DbContext/Migrationen vorhanden; Importpfad und Betriebsnachweis fehlen |
| Data Product Layer | ⬜ | keine Ports, Verträge oder Publisher |

## F) Roadmap bis Version 1.0

Die folgenden Arbeiten schließen verbleibende Lücken zur bestehenden Baseline. Sie erweitern die Zielarchitektur nicht.

### 1. Resolution- und Schreibpfad härten

- authentifizierten Benutzerkontext statt übergebener UserId integrieren;
- Audit Trail unveränderlich in produktivem Storage speichern;
- fachliches Mapping und Transaktionen im `DatabaseImportWriter` implementieren;
- dateibasierten `RawZoneWriter` um Retention und Zugriffsschutz ergänzen;
- Idempotenz, Retry und Abbruchverhalten testen.

### 2. ImportReport und Import History persistieren

- dateibasiertes Repository durch produktives Datenbankrepository mit Concurrency Control ergänzen;
- Wiederaufnahme und konkurrierende Änderungen absichern;
- Import History und technische Provenance bereitstellen.

### 3. REST API und Dokumentation

- bestehende Analyse-, Report-, Resolution- und Commit-Endpunkte härten;
- Validierung, Fehlerverträge und OpenAPI ergänzen;
- Authentifizierung und Autorisierung integrieren.

### 4. React Import Wizard

- Upload, Analysefortschritt und Reportanzeige implementieren;
- Issue-Resolution und explizite Bestätigung abbilden;
- UI- und End-to-End-Tests ergänzen.

### 5. Background Jobs und Betrieb

- Worker auf Generic Host und externe Konfiguration umstellen;
- Queueing, Scheduling, Retry und Monitoring ergänzen;
- Docker/Compose und CI/CD bereitstellen.

### 6. Tests und Baseline-Nachweis

- Unit Tests für Coordinator, DecisionEngine, ResolutionService und WriteGate;
- IntegrationsTests für Reader, Writer, Datenbank und Raw Zone;
- Architekturtests für Layer- und Package-Grenzen;
- API-, Sicherheits- und End-to-End-Tests;
- abschließendes Compliance-Review gegen Version 1.0.

## G) Architekturbewertung

### Bereits vollständig entsprechend der Baseline umgesetzt

- grundlegende Clean-Architecture-Abhängigkeitsrichtung;
- Application-seitige Orchestrierung der Importanalyse;
- Trennung der Analyse von Benutzerentscheidung und Schreiben;
- kein Schreibzugriff nach Analyse-`Abort` im aktiven Workerpfad;
- zentrale typisierte `ImportIssue`-Struktur;
- interne Kapselung von `DuplicateCandidate`;
- ClosedXML ausschließlich in Infrastructure;
- Reader-/Writer-Ports in Application;
- Context-basiertes WriteGate.

### Bewusst verschoben oder noch nicht integriert

- API- und UI-gesteuerter Resolution-Ablauf;
- persistente Reports, Import History und Audit;
- produktive Database-/Raw-Zone-Writer;
- Background Jobs und Hosted Worker;
- vollständige Importunterstützung für alle Entitätstypen;
- Data-Product-Pipeline.

### Aktuelle technische Schulden

- hart codierter lokaler Excel-Pfad im Worker;
- noch unvollständige Testabdeckung und keine Solution-Datei;
- ältere versionierte `bin`-/`obj`-Artefakte; `.gitignore` schützt nur neue Artefakte;
- leere Mapper-, Validator-, AutoFix- und Normalizer-Platzhalter;
- teilweise uneinheitliche Ordner-/Namespace- und Codeformatierung;
- synchroner Datei-I/O hinter einer asynchron benannten Coordinator-Schnittstelle;
- keine externe Konfiguration, kein Generic Host und kein strukturiertes Logging;
- Audit und Resolution-Zustand sind nur dateibasiert gespeichert, ohne unveränderliche DB-Historie oder Concurrency Control;
- TimescaleDB-Hypertable und produktiver Datenbankbetrieb nicht nachgewiesen.

## Build- und Testnachweis

Der aktuelle Gesamtbuild über das Worker-Projekt wurde erfolgreich ausgeführt:

```text
dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
Build erfolgreich, 0 Warnungen, 0 Fehler
dotnet build src/Enset.Api/Enset.Api.csproj --no-restore
Build erfolgreich, 0 Warnungen, 0 Fehler
dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj --no-restore
7 von 7 Tests bestanden
```

Die vorhandenen Tests decken die zentralen Phase-2-Grenzen ab. Excel-/API-End-to-End-, Datenbank-, Raw-Zone-, Sicherheits- und Concurrency-Tests bleiben offen.
