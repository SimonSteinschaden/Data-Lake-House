# ENSET Data Lake House – Architecture Review v1.2

**Prüfdatum:** 2026-07-06  
**Prüfgegenstand:** aktueller Repository-Stand nach Importarchitektur Phase 2  
**Verbindliche Referenz:** `ARCHITECTURE_REVIEW_V1_0.md` / ENSET Architecture Baseline v1.0  
**Vorgängerreview:** `ARCHITECTURE_REVIEW_V1_1.md`  
**Charakter:** IST-Review; keine Änderung oder Erweiterung der Zielarchitektur

## 1. Executive Summary

Phase 2 setzt den zweistufigen Importworkflow technisch durch:

```text
Analyse:
Excel -> ImportAnalysisService -> ImportCoordinator -> ImportReport -> Repository

Commit:
User Decisions -> ApplyResolutionService -> Repository
               -> ImportCommitService -> ImportWriteContext
               -> ImportWriteGate -> IImportWriter -> optional RawZoneWriter
```

Der `ImportCoordinator` bleibt strikt analyse-only. Er besitzt keine Abhängigkeit auf Gate oder Writer und führt keine Schreiboperation aus. REST API und Console-Test-Runner verwenden denselben Application-Commit-Pfad. Controller rufen keinen Writer direkt auf.

Gegenüber v1.1 wurden insbesondere ergänzt:

- persistierbarer `ImportReport` mit Status, SourceFile-Metadaten, Zeitstempeln und Audit Trail;
- `IImportReportRepository` mit dateibasierter JSON-Implementierung;
- auditierbare und vor Commit mehrfach änderbare Issue-Resolutionen;
- zentraler `ImportCommitService`;
- strukturiertes `ImportWriteGateResult`;
- erweiterter `ImportWriteContext` mit Zielmodus und Zielwriter;
- ASP.NET-Core-REST-API mit vier Importendpunkten;
- angebundener Excel-Writer und dateibasierter Raw-Zone-Writer;
- vorbereiteter, sicher blockierender Database-Writer;
- gemeinsamer Console-Testpfad;
- sieben automatisierte Architektur- und Workflowtests.

Die Baseline v1.0 ist noch nicht vollständig erfüllt. Die größten Lücken sind fachliches Database-Mapping, produktive relationale Report-/Audit-Persistenz, Authentifizierung, React UI, Background Jobs, OpenAPI und breitere Integrations-/End-to-End-Tests.

## 2. Projektstruktur und Abhängigkeiten

| Projekt | Verantwortung | Referenzen |
|---|---|---|
| `Enset.Domain` | Domain-Entities, Enums und fachliche Basistypen | keine |
| `Enset.Application` | Use Cases, Ports, Importmodelle, Resolution, Commit und Gate | Domain |
| `Enset.Infrastructure` | EF Core/Npgsql, Excel, JSON-Persistenz, Raw Zone und Writer | Application, Domain |
| `Enset.Worker` | Konsolen-Composition-Root und Entwickler-Testpfad | Application, Infrastructure, Domain |
| `Enset.Api` | REST Controller, API-Mapping und Composition Root | Application, Infrastructure |
| `Enset.Import.Tests` | Architektur- und Workflowtests | API, Application, Infrastructure |

Die Bibliotheksreferenzen folgen weiterhin der Clean-Architecture-Richtung. ClosedXML und EF Core sind ausschließlich in Infrastructure referenziert. API und Worker dürfen konkrete Adapter am Composition Root instanziieren; die fachliche Ablaufsteuerung liegt in Application.

## 3. Analysepfad

### 3.1 IImportAnalysisService und ExcelImportAnalysisService

`IImportAnalysisService` abstrahiert den API-nahen Analyse-Use-Case. `ExcelImportAnalysisService`:

1. speichert den Upload in einem Staging-Verzeichnis;
2. berechnet SHA-256 und Dateimetadaten;
3. erstellt den Excel-Reader und den Application-Coordinator;
4. führt ausschließlich die Analyse aus;
5. ergänzt SourceFile und Audit Trail;
6. speichert den Report über `IImportReportRepository`.

Die Staging-Datei wird nicht automatisch gelöscht. Retention und Bereinigung sind noch offen.

### 3.2 ImportCoordinator

Der Coordinator orchestriert unverändert:

```text
Read -> Map -> Validate -> DuplicateCheck -> Decision -> ImportReport
```

Abhängigkeiten:

- `IImportReader`
- `IImportMapper`
- `IImportValidator`
- `IDuplicationCheckService`
- `IImportLogger`

Nicht vorhanden sind `IImportWriteGate`, `IImportWriter`, Repository- oder Benutzerinteraktionsabhängigkeiten.

### 3.3 ImportReport

Der aktuelle Report enthält:

- eindeutige `ImportId`;
- `ImportStatus`;
- `ImportSourceFileMetadata` mit Dateiname, ContentType, Länge, SHA-256, internem Stagingpfad und optionalem Raw-Pfad;
- gemappte Customers;
- `ImportIssue`-Liste;
- `ImportDecision`;
- `CreatedAt` und `UpdatedAt`;
- `ImportAuditEntry`-Liste;
- abgeleitete Statistiken und Severity-Sichten.

Interne Staging- und Raw-Pfade werden durch das API-Response-Mapping nicht veröffentlicht.

## 4. Reportpersistenz und Audit

### 4.1 IImportReportRepository

Der Application-Port stellt asynchrones Speichern und Laden über `ImportId` bereit. Dadurch können API, UI und Console denselben Reportzustand verwenden.

### 4.2 JsonImportReportRepository

Die aktuelle Infrastructure-Implementierung:

- speichert jeden Report als eigene JSON-Datei;
- serialisiert Enums lesbar als Strings;
- schreibt zunächst eine temporäre Datei und ersetzt danach das Ziel;
- synchronisiert Zugriffe innerhalb einer Repository-Instanz mit `SemaphoreSlim`.

Grenzen:

- keine prozessübergreifende Sperre;
- keine optimistische Concurrency oder Versionsnummer;
- kein relationales Querying;
- Auditdateien sind technisch veränderbar;
- keine Retention-, Backup- oder Berechtigungsstrategie.

### 4.3 Audit Trail

Analyse, Resolution und Commit erzeugen Audit-Einträge. Resolutionen speichern IssueId, Benutzer, Zeitpunkt, vorherige und neue Action sowie Custom Values. Commit protokolliert Start, Erfolg oder Fehler.

Der Audit Trail erfüllt die Workflow-Nachvollziehbarkeit des Prototyps, ist aber noch kein unveränderliches produktives Audit-System.

## 5. Resolutionpfad

`ApplyResolutionService`:

- akzeptiert ausschließlich Issues des geladenen Reports;
- verhindert doppelte Resolutionen innerhalb eines Requests;
- akzeptiert nur Issues mit `RequiresUserDecision`;
- validiert Custom Values;
- erlaubt wiederholte Änderungen vor `Committing` oder `Committed`;
- erlaubt mit Action `None` das Zurücksetzen einer Entscheidung;
- aktualisiert `IsResolved`, Decision, Status und UpdatedAt;
- schreibt Audit-Einträge;
- führt keine Writer- oder Persistenzoperation selbst aus.

Der API-Controller speichert den aktualisierten Report anschließend über das Repository.

## 6. Zentraler Commit-Pfad

### 6.1 ImportWriteContext

Der Context enthält:

- ImportId und vollständigen Report;
- `ImportTargetMode` (`Upsert` oder `Replace`);
- `ImportWriterType` (`Excel` oder `Database`);
- UserId und Timestamp;
- optionale TargetLocation;
- Raw-Zone-Option;
- abgeleitete Customers und Issues.

### 6.2 ImportWriteGate

Das Gate erlaubt den Commit nur, wenn:

- ein Report vorhanden ist;
- Context-ImportId und Report-ImportId übereinstimmen;
- ein User-Kontext vorhanden ist;
- der Reportstatus `ReadyToCommit` lautet;
- die technische Decision nicht `Abort` ist;
- keine entscheidungspflichtigen Issues offen sind;
- für Excel ein Ziel angegeben ist.

`ImportWriteGateResult` liefert alle Gate-Fehler. Das Gate ruft keinen Writer auf.

### 6.3 ImportCommitService

Nur dieser Application-Service orchestriert den Commit:

1. Report über ImportId laden;
2. WriteContext erzeugen;
3. Gate vollständig auswerten;
4. passenden `IImportWriter` auswählen;
5. Status `Committing` und Audit speichern;
6. Writer ausführen;
7. optional Originaldatei in die Raw Zone archivieren;
8. Status `Committed` oder `Failed` und Audit speichern.

API und `DuplicationResolutionRunner` verwenden diesen Service. Es gibt keinen separaten Console-Commitalgorithmus.

Aktuelle Grenze: Ziel-Write und nachgelagerte Raw-Archivierung sind nicht atomar. Schlägt die Raw-Archivierung nach einem erfolgreichen Ziel-Write fehl, wird der Report `Failed`, obwohl das Ziel bereits verändert sein kann.

## 7. Writer

### 7.1 ExcelImportWriter

Der Excel-Writer:

- implementiert `IImportWriter` für das Ziel `Excel`;
- verwendet ausschließlich den freigegebenen Context;
- kopiert die gestagte Quelle an das Ziel;
- verhindert das Überschreiben der gestagten Originaldatei;
- begrenzt API-Ziele auf das konfigurierte Output-Verzeichnis;
- delegiert Feldänderungen an `ExcelWorkbookWriter`.

### 7.2 DatabaseImportWriter

Der Writer ist als `IImportWriter` für das Ziel `Database` registrierbar. Er führt bewusst keine Datenänderung aus und wirft `NotSupportedException`, solange ein eindeutiges fachliches Mapping von Import-DTOs auf Domain-Entities und ein Transaktionskonzept fehlen. Dadurch entsteht kein scheinbar erfolgreicher Teilimport.

### 7.3 FileSystemRawZoneWriter

Der Raw-Zone-Writer kopiert die gestagte Originaldatei in ein Verzeichnis je ImportId. Der Dateiname enthält SHA-256 und Originalnamen. Der resultierende interne Pfad wird im Report gespeichert.

Noch offen sind erneute Hash-Verifikation beim Archivieren, produktiver Object Storage, Retention, Unveränderlichkeit und Zugriffsschutz.

## 8. REST API

`Enset.Api` stellt bereit:

| Methode und Route | Verhalten |
|---|---|
| `POST /api/v1/imports/analyze` | Excel-Upload analysieren und Report speichern |
| `GET /api/v1/imports/{importId}` | gespeicherten Report abrufen |
| `POST /api/v1/imports/{importId}/resolutions` | Entscheidungen anwenden und auditieren, ohne Nutzdaten zu schreiben |
| `POST /api/v1/imports/{importId}/commit` | zentralen Commit-Service aufrufen |

`ImportsController` hängt von AnalysisService, ReportRepository, ResolutionService und CommitService ab. Er besitzt keine direkte Writer- oder Gate-Abhängigkeit.

Aktuelle API-Grenzen:

- keine Authentifizierung; UserId kommt aus Header bzw. Request;
- keine Autorisierung oder Freigaberollen;
- kein OpenAPI/Swagger;
- keine standardisierten ProblemDetails-Verträge für alle Fehler;
- keine Uploadgrößen-, Malware- oder echte Content-Prüfung;
- noch keine HTTP-Integrations- oder Sicherheitstests.

## 9. Tests und Buildnachweis

Das Projekt `Enset.Import.Tests` enthält sieben Tests für:

- analyse-only Coordinator-Grenze;
- Speichern und Laden eines Reports;
- wiederholbare und auditierte Resolutionen;
- Gate blockiert offene Issues;
- Gate erlaubt resolved Issues;
- Commit ruft Writer erst nach erfolgreichem Gate auf;
- API-Controller besitzt keine direkte Writer-Abhängigkeit.

Nachweis:

```text
dotnet build src/Enset.Api/Enset.Api.csproj --no-restore
Erfolgreich, 0 Warnungen, 0 Fehler

dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
Erfolgreich, 0 Warnungen, 0 Fehler

dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj --no-restore
7 von 7 Tests bestanden
```

Die Tests sichern zentrale Architekturgrenzen, ersetzen aber noch keine realen Excel-, HTTP-, Datenbank-, Raw-Zone-, Concurrency- oder End-to-End-Tests.

## 10. Implementierungsstatus

Legende: ✅ implementiert, 🟡 teilweise implementiert, ⬜ offen

| Modul | Status | Befund |
|---|:---:|---|
| Analyse-Coordinator | ✅ | Application-gesteuert und ohne Write-Abhängigkeit |
| Excel ImportReader | ✅ | Stream-/Workbook-Adapter vorhanden |
| Mapping | 🟡 | aktive Customer-Abbildung; weitere Entitäten unvollständig |
| Validation | 🟡 | Customer-/Building-Regeln vorhanden; Platzhalter und breitere Regeln offen |
| DuplicateCheck | 🟡 | Customer-Workflow vorhanden; weitere Entitäten offen |
| ImportReport | ✅ | Status, Source, Audit und Statistiken vorhanden |
| Reportrepository | 🟡 | JSON-Persistenz vorhanden; produktives DB-Backend fehlt |
| ApplyResolutionService | ✅ | wiederholbar, auditierbar, ohne Writerlogik |
| ImportWriteContext | ✅ | Report, User, Timestamp, Modus und Zielwriter enthalten |
| ImportWriteGate | ✅ | zentrale Validierung mit strukturiertem Ergebnis |
| ImportCommitService | ✅ | gemeinsamer API-/Console-Commitpfad |
| ExcelImportWriter | ✅ | Commit-integriert und gegen Source-Überschreiben geschützt |
| DatabaseImportWriter | 🟡 | Vertrag und sichere Blockade vorhanden; Mapping/Transaktion offen |
| RawZoneWriter | 🟡 | dateibasiert vorhanden; produktive Storage-Eigenschaften offen |
| REST API | 🟡 | vier Endpunkte vorhanden; Auth/OpenAPI/Hardening offen |
| React UI | ⬜ | nicht vorhanden |
| Authentication/Authorization | ⬜ | nicht vorhanden |
| Import History | 🟡 | Report-Audit vorhanden; relationale Historie und Queries fehlen |
| Background Jobs | ⬜ | nicht vorhanden |
| Automatisierte Tests | 🟡 | sieben Kernprüfungen; Integration/E2E unvollständig |
| Data Product Layer | ⬜ | nicht implementiert |

## 11. Bewertung gegen Baseline v1.0

### Vollständig oder für Phase 2 ausreichend umgesetzt

- Application-gesteuerte Importorchestrierung;
- strikte Trennung von Analyse und Commit;
- typisierte Issues und explizite ResolutionActions;
- keine Writeraufrufe im Coordinator oder Controller;
- zentraler Context-/Gate-/Writer-Pfad;
- keine impliziten oder automatischen Dublettenmerges;
- Excel/ClosedXML ausschließlich in Infrastructure;
- gemeinsamer fachlicher Flow für API und Console-Test-Runner;
- gespeicherter Reportzustand und prototypischer Audit Trail;
- automatisierte Prüfung zentraler Architekturgrenzen.

### Teilweise umgesetzt

- Report- und Auditpersistenz: JSON statt produktivem Datenbankmodell;
- Raw Zone: Dateisystem statt produktivem unveränderlichem Storage;
- Writer: Excel funktionsfähig, Database bewusst blockiert;
- API: fachliche Endpunkte vorhanden, Querschnittsthemen fehlen;
- Tests: Kernlogik abgedeckt, Adapter und End-to-End offen.

### Weiterhin offen

- React Import Wizard;
- Authentifizierung, Autorisierung und Benutzerrollen;
- DatabaseImportWriter mit Domain-Mapping und Transaktion;
- relationale Import History, Concurrency und Idempotenz;
- Background Jobs, Queueing, Retry und Wiederaufnahme;
- produktive Raw-/Curated-/Data-Product-Pipeline;
- OpenAPI, Monitoring, Deployment und CI/CD.

## 12. Technische Schulden und Risiken

- `Program.cs` des Workers enthält weiterhin einen hart codierten Entwicklungsdateipfad.
- Dateibasierte Reports besitzen keine prozessübergreifende Concurrency Control.
- Auditdaten sind nicht unveränderlich gespeichert.
- Ziel-Write und Raw-Archivierung bilden keine gemeinsame Transaktion.
- Der DatabaseWriter ist absichtlich noch nicht funktionsfähig.
- UserId ist nicht kryptografisch an einen authentifizierten Benutzer gebunden.
- Gestagte Uploads besitzen keine Retention oder Bereinigung.
- Uploadinhalt wird nur über Dateiendung, nicht über sicheren Content-Scan geprüft.
- Coordinator und Reader arbeiten intern synchron hinter einer Task-basierten Schnittstelle.
- Mehrere Mapper-, Validator-, Normalizer- und AutoFix-Dateien sind leere Platzhalter.
- Eine Solution-Datei fehlt.
- Ältere `bin`-/`obj`-Artefakte sind bereits versioniert; `.gitignore` schützt nur neue Artefakte.

## 13. Nächste Baseline-Arbeiten

Ohne die Zielarchitektur zu verändern, verbleiben als nächste Schritte:

1. authentifizierten User-Kontext und Autorisierung einführen;
2. DatabaseImportWriter fachlich und transaktional implementieren;
3. JSON-Repository durch ein produktives Repository mit Concurrency ersetzen;
4. Raw-Zone-Storage hinsichtlich Hash-Verifikation, Retention und Unveränderlichkeit härten;
5. React Import Wizard auf den bestehenden REST-Flow setzen;
6. OpenAPI und standardisierte Fehlerverträge ergänzen;
7. HTTP-, Excel-, Datenbank-, Raw-Zone-, Concurrency- und End-to-End-Tests ergänzen;
8. Background Jobs, Idempotenz, Retry und Importwiederaufnahme implementieren;
9. Data-Product- und Curated-Pfade der Baseline umsetzen.

## 14. Gesamturteil

Phase 2 setzt die zentrale Sicherheitsregel der Baseline technisch deutlich stärker durch: Analyse und Schreiben sind getrennt, Resolutionen werden gespeichert und auditiert, und Writer sind nur über den zentralen Commit-Service nach erfolgreichem Gate erreichbar.

Der Stand ist ein belastbarer Architekturprototyp, aber noch keine produktionsreife Version 1.0. Die verbleibenden Risiken liegen weniger in der Ablaufstruktur als in Persistenzqualität, Transaktionsgrenzen, Security, Betrieb und vollständiger fachlicher Datenbankintegration.
