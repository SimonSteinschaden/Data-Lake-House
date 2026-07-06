# ENSET Data Lake House – Architecture Review v1.1

**Prüfdatum:** 2026-07-06  
**Prüfgegenstand:** aktueller Implementierungsstand des Repository  
**Verbindliche Referenz:** `ARCHITECTURE_REVIEW_V1_0.md` / ENSET Architecture Baseline v1.0  
**Charakter dieses Dokuments:** reiner IST-Review, keine neue Zielarchitektur

## Einordnung

Seit dem Review v1.0 wurde der Importworkflow deutlich näher an die Baseline geführt. Die Importanalyse wird nun in Application orchestriert und ist vom Resolution-/Schreibpfad getrennt. Zentrale Workflowmodelle und Gates sind vorhanden. Die Baseline v1.0 ist trotzdem noch nicht vollständig umgesetzt: Der zweite Pfad ist nicht produktiv integriert, und API, UI, persistente Reports, Storage-Writer, Betrieb und Tests fehlen.

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

Der Report ist noch flüchtig und wird nicht persistiert.

### ImportDecisionEngine

`ImportDecisionEngine` bewertet den Analyse-Report. Bei mindestens einem Error oder Critical Issue erzeugt er `Abort`, sonst `Continue`, jeweils mit einer Begründung. Diese Entscheidung ist keine Benutzerfreigabe.

### ApplyResolutionService

`IApplyResolutionService` und `ApplyResolutionService` sind implementiert. Der Service ordnet Benutzerentscheidungen über `IssueId` zu, verhindert doppelte Resolutionen, validiert Action und Custom Value, markiert entscheidungspflichtige Issues als resolved und erzeugt einen `ImportWriteContext`.

Der Service ist noch nicht über API, UI oder einen produktiven Workerpfad erreichbar. Benutzeridentität, Zeitstempel und persistenter Audit Trail fehlen.

### ImportWriteContext

`ImportWriteContext` bündelt die für eine Schreibfreigabe relevanten Informationen:

- aktuelle `ImportDecisionType`;
- `UserConfirmed`;
- Issues samt Resolution-Zuständen;
- freizugebende Customer-DTOs.

### ImportWriteGate

`IImportWriteGate` abstrahiert die Freigabe. `ImportWriteGate` erlaubt Schreiben nur, wenn die Decision nicht `Abort` ist, eine Benutzerbestätigung vorliegt und keine entscheidungspflichtigen Issues ungelöst sind. Das Gate bewertet ausschließlich den `ImportWriteContext` und schreibt selbst nichts.

### IImportWriter und ExcelImportWriter

`IImportWriter` ist der Application-Port für einen freigegebenen Schreibvorgang. `ExcelImportWriter` ist der vorhandene Infrastructure-Adapter und delegiert die technische Aktualisierung an `IExcelWorkbookWriter`. Im aktiven Workerpfad wird kein Writer aufgerufen.

### ExcelWorkbookWriter

`ExcelWorkbookWriter` verwendet ClosedXML, öffnet eine konfigurierte Arbeitsmappe und aktualisiert passende Customer-Zeilen anhand der externen Customer-ID. Der Writer ist vorhanden, aber nicht in einen produktiven, durchgängig freigegebenen Importablauf integriert.

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

Die Kernbausteine dieses Teils existieren. Noch offen sind der persistente Reportabruf, ein API-/UI- oder Worker-Aufrufer, Auditdaten und die produktive Verdrahtung. Das WriteGate darf erst nach angewendeten Benutzerentscheidungen aufgerufen werden.

## C) Projektstruktur

| Layer | Aktuelle Verantwortung | Technische Grenze |
|---|---|---|
| Application | Import-Use-Case, Ports, DTOs, Validierung, DuplicationCheck, Reports, Decisions, Resolution und WriteGate | referenziert nur Domain; kein ClosedXML |
| Infrastructure | EF Core/Npgsql, Excel-/CSV-Adapter, Workbook-Zugriff, Lookup und Migrationen | referenziert Application und Domain; enthält konkrete I/O-Packages |
| Worker | Composition Root, Entwicklungskonfiguration, Runner, Konsolenlogger und Reportausgabe | referenziert die drei Bibliotheken; kein produktiver Host |
| Domain | Entities, Enums und fachliche Basistypen | keine Projekt- oder Package-Referenzen |

Nicht vorhanden sind API-, UI- und Testprojekte sowie eine Solution-Datei.

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
| ImportWriter | 🟡 | Excel-Adapter vorhanden, nicht produktiv verdrahtet; DB/Raw fehlen |
| Logging | 🟡 | Port und Console-Adapter vorhanden; strukturiertes Logging fehlt |
| Validation | 🟡 | aktive Excel-Customer-/Building-Prüfungen; weitere Validatoren und Tests fehlen |
| DuplicateCheck | 🟡 | Customer-Erkennung und Issue-Mapping vorhanden; weitere Entitäten/Audit fehlen |
| DecisionEngine | ✅ | technische Continue-/Abort-Entscheidung vorhanden |
| ImportReport | 🟡 | UI-/API-fähiges Modell vorhanden; Persistenz fehlt |
| ApplyResolutionService | 🟡 | Kernservice vorhanden; API/UI/Audit/Integration fehlen |
| ImportWriteGate | ✅ | Context-basierte Freigabelogik vorhanden |
| REST API | ⬜ | nur vorbereitende DTOs vorhanden |
| React UI | ⬜ | kein Frontend-Projekt |
| DatabaseWriter | ⬜ | nicht implementiert |
| RawZoneWriter | ⬜ | nicht implementiert |
| Authentication | ⬜ | nicht implementiert |
| ImportHistory | ⬜ | nicht implementiert |
| Audit | ⬜ | keine persistente Benutzer-/Entscheidungshistorie |
| Background Jobs | ⬜ | kein Hosted Worker, Queueing oder Scheduling |
| Automatisierte Tests | ⬜ | keine Testprojekte vorhanden |
| EF-Core-Domainpersistenz | 🟡 | DbContext/Migrationen vorhanden; Importpfad und Betriebsnachweis fehlen |
| Data Product Layer | ⬜ | keine Ports, Verträge oder Publisher |

## F) Roadmap bis Version 1.0

Die folgenden Arbeiten schließen verbleibende Lücken zur bestehenden Baseline. Sie erweitern die Zielarchitektur nicht.

### 1. Resolution- und Schreibpfad fertigstellen

- `ApplyResolutionService` in einen persistenten Use Case integrieren;
- Entscheidungen mit Benutzeridentität, Zeitpunkt und Audit Trail speichern;
- `ImportReport -> ApplyResolutionService -> WriteGate -> Writer` produktiv verdrahten;
- transaktionalen `DatabaseImportWriter` und unveränderlichen `RawZoneWriter` implementieren;
- Idempotenz, Retry und Abbruchverhalten testen.

### 2. ImportReport und Import History persistieren

- Report, Status und Quelldatei-Referenz speichern;
- Abruf und Wiederaufnahme über `ImportId` ermöglichen;
- Import History und technische Provenance bereitstellen.

### 3. REST API und Dokumentation

- ASP.NET-Core-API-Projekt hinzufügen;
- Analyse-, Report- und Resolution-Endpunkte implementieren;
- DTO-Mapping, Validierung, Fehlerverträge und OpenAPI ergänzen;
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
- keine automatisierten Tests und keine Solution-Datei;
- versionierte `bin`-/`obj`-Artefakte sowie keine wirksame `.gitignore`-Bereinigung;
- leere Mapper-, Validator-, AutoFix- und Normalizer-Platzhalter;
- teilweise uneinheitliche Ordner-/Namespace- und Codeformatierung;
- synchroner Datei-I/O hinter einer asynchron benannten Coordinator-Schnittstelle;
- keine externe Konfiguration, kein Generic Host und kein strukturiertes Logging;
- kein Audit Trail und keine Persistenz des Resolution-Zustands;
- TimescaleDB-Hypertable und produktiver Datenbankbetrieb nicht nachgewiesen.

## Build- und Testnachweis

Der aktuelle Gesamtbuild über das Worker-Projekt wurde erfolgreich ausgeführt:

```text
dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
Build erfolgreich, 0 Warnungen, 0 Fehler
```

Es existieren weiterhin keine Testprojekte; ein automatisierter Verhaltensnachweis ist daher noch nicht möglich.
