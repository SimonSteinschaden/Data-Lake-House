# Architecture Review Version 1.2

**Prüfdatum:** 19.07.2026
**Prüfgegenstand:** aktueller Arbeitsbaum des Repositorys `ENSET Data Lake House` einschließlich nicht committeter Änderungen
**Charakter:** belastbarer IST-Review; keine Produktivcodeänderung und kein Refactoring
**Verbindliche Grundlage:** die im Auftrag formulierten Architekturgrundsätze; vorhandene Dokumentation wurde nur als Vergleichsquelle verwendet

> Hinweis zum Prüfstand: `git status --short --branch` zeigte bereits vor dem Review zahlreiche geänderte, gelöschte und neue Dateien sowie veränderte versionierte Buildartefakte. Diese Änderungen werden als vorhandene Benutzerarbeit behandelt. Aussagen beziehen sich auf den Arbeitsbaum, nicht ausschließlich auf `HEAD`. Vermutungen sind ausdrücklich markiert.

## 1. Executive Summary

Die grundlegende Projektrichtung ist tragfähig: `Enset.Domain` ist technisch unabhängig, `Enset.Application` hängt nur von Domain ab, und Infrastructure implementiert Application-Ports. Analyse und Commit sind konzeptionell getrennt; API-Controller schreiben nicht direkt, und `ImportCommitService` ruft Writer erst nach `ImportWriteGate` auf. API, Worker und React/Vite sind eigenständige Anwendungen. API-Build, React-Build und ESLint bestehen.

Der aktuelle Stand ist dennoch nicht Version-1.0- oder produktionsreif. Das höchste fachliche Risiko liegt zwischen Resolution und Write: `ApplyResolutionService` ändert nur Metadaten am `ImportIssue`; `report.Customers` wird nicht entsprechend `KeepFirst`, `KeepSecond`, `UseCustomValue` oder `KeepSeparate` transformiert. Danach kann das Gate den unveränderten Payload freigeben. Parallel dazu fehlen ein zentral durchgesetztes Statusmodell, Idempotenz und Concurrency Control. Zwei gleichartige Report-Repositories haben inkompatible Speicherseman­tik; aktiv ist weiterhin die lokale JSON-Variante. Das EF-Repository verliert beim Mapping unter anderem Customers und Decision und kann den von API/Commit erwarteten wiederholten `SaveAsync`-Pfad nicht bedienen.

Die Klassen- und Ordneranzahl ist nicht grundsätzlich zu hoch. Wesentliche Ports, Pipeline-Schritte, API-Verträge und Adapter sind fachlich gerechtfertigt. Accidental Complexity entsteht dagegen durch 13 leere Quelldateien, mehrere tote oder parallele Altpfade, doppelte Excel-Abstraktionen, zwei DbContext-Dateien bzw. Namespaces, generische technische Ordner innerhalb des Importfeatures und 24 Interfaces mit teils fehlender aktiver Consumer-Grenze. Das erschwert Discoverability stärker als die reine Dateianzahl.

Der Worker und die Tests kompilieren aktuell wegen der unvollständigen Umbenennung `ImportCommitRequest` → `ImportCommitCommand` nicht. Das Frontend ist strukturell ordentlich begonnen und kapselt den generierten NSwag-Client, führt aber noch keine Analyse, Resolution oder Commit-Operation gegen die API aus; es zeigt Mockdaten und lokale Zustandswechsel als Erfolg.

**Gesamtempfehlung: B – einzelne Module gezielt restrukturieren.** Die Projektgrenzen Domain/Application/Infrastructure/API/Worker/Web sollen erhalten bleiben. Gezielte Restrukturierung ist im Import-Lifecycle, in Reportpersistenz, Worker-Composition und Frontend-State nötig; eine repositoryweite Neuarchitektur wäre unverhältnismäßig.

## 2. Repositoryübersicht

| Projekt/Bereich | Tatsächliche Verantwortung | Abhängigkeiten | Bewertung der Grenze |
|---|---|---|---|
| `src/Enset.Domain` | Entities und Enums für Kunden, Projekte, Gebäude, Energie, Dokumente, Analytics | keine Projekt-/Package-Referenzen | korrekt unabhängig; derzeit überwiegend anämisches Datenmodell |
| `src/Enset.Application` | Importmodelle, Ports, Analyse-Orchestrierung, Validation, Duplicate Check, Resolution, Commit und WriteGate | Domain | Richtung korrekt; Importfeature intern fragmentiert und enthält tote Vorbereitungen |
| `src/Enset.Infrastructure` | ClosedXML, CSV, EF Core/Npgsql, Reportpersistenz, Raw-Zone und Writer | Application, Domain | als Adapterlayer richtig; aktive und experimentelle Pfade vermischt |
| `src/Enset.Api` | REST-Verträge, Mapping, ProblemDetails-Helfer, Controller und Composition Root | Application, Infrastructure | deploybar und schlank; Controller orchestriert Resolution plus Persistenz selbst |
| `src/Enset.Worker` | aktuelle Konsolenanalyse und Entwickler-Testharness | Domain, Application, Infrastructure | eigenständig, aber kein Hosted Worker; harter Windows-Pfad und eigener manueller Object Graph |
| `src/Enset.Web` | React/Vite-Router, Layout, Import-Wizard, Servicekapsel und generierter NSwag-Client | HTTP-Vertrag | eigenständig buildbar; API-Integration im Feature noch nicht aktiv |
| `tests/Enset.Import.Tests` | sieben xUnit-Architektur-/Workflowtests | API, Application, Infrastructure | sinnvolle Kernchecks, aber derzeit nicht kompilierbar und zu breit gekoppelt |
| `docs` | Baseline, historische Reviews und IST-Dokumentation | – | teilweise deutlich hinter dem Arbeitsbaum |

Der statische Projektgraph folgt `Domain <- Application <- Infrastructure <- Hosts`. Dass API und Worker Infrastructure referenzieren, ist am Composition Root legitim. Eine Solution-Datei, zentrale Buildkonfiguration (`Directory.Build.props`), CI-Konfiguration sowie Container-/Kubernetes-Manifeste fehlen. Im Repository liegen 182 produktionsnahe C#-Dateien und 33 TS/TSX-Dateien außerhalb des generierten Clients; außerdem sind zahlreiche `bin`/`obj`-Artefakte bereits versioniert.

Erkannte fachliche Grenzen sind Importanalyse, Report/Issue-Workflow, Resolution, Commit/WriteGate, Writer/Raw Zone, API-Vertrag und React-Importfeature. Data Products, Integration Layer, Auth/Mandant und ein produktiver asynchroner Worker sind noch keine implementierten Grenzen.

## 3. Kritische Inkonsistenzen

| ID | Priorität | Datei/Klasse | Problem | Risiko | Empfehlung |
|---|---|---|---|---|---|
| C-01 | P0 – Defekt | `src/Enset.Application/Imports/DuplicationCheck/Resolutions/ApplyResolutionService.cs`, `Apply` | Resolution ändert nur Issue-Zustand; der zu schreibende Payload wird nicht aufgelöst | Gate erlaubt fachlich unveränderte oder falsche Daten | explizites `ResolvedImportPayload`/Resolution-Result erzeugen; Writer darf nur dieses Ergebnis konsumieren; Tests je Action vor Änderung |
| C-02 | P0 – Defekt | `src/Enset.Worker/Tests/DuplicationResolutionRunner.cs:17`, `tests/Enset.Import.Tests/ImportArchitectureTests.cs:226` | alte Klasse `ImportCommitRequest` nach Umbenennung auf `ImportCommitCommand` referenziert | Worker-Build und gesamter Testlauf scheitern | Umbenennung vollständig und atomar abschließen; Compile-Check aller Hosts in CI |
| C-03 | P0 – Architekturinkonsistenz | `ImportCommitService.CommitAsync`, `IImportReportRepository` | Read-modify-write ohne Versionsprüfung oder Sperre; Status wird erst nach Gate gesetzt | zwei Requests können beide Gate passieren und zweimal schreiben; Resolution kann Commit überschreiben | Reportversion/ETag und atomaren `TryTransitionAsync(expectedVersion, from, to)`-Port einführen; DB-Transaktion/Lease für Commit |
| C-04 | P0 – Defekt bei EF-Aktivierung | `EfImportReportRepository.SaveAsync` | `SaveAsync` darf nur Insert, während alle Consumer dieselbe Methode auch für Updates verwenden | Analysis funktioniert, Resolution/Commit schlagen danach mit „already exists“ fehl | Port in `Add`/`Get`/`Save(expectedVersion)` oder echtes Upsert mit Concurrency aufteilen; Consumervertrag testen |
| C-05 | P0 – Defekt bei EF-Aktivierung | `ImportReportPersistenceMapper.ToEntity/ToModel` | Customers und `ImportDecision` werden nicht persistiert/rehydriert; Update löscht und ersetzt Audit/Issues | DB-Commit schreibt leeren Payload; Audit ist veränderbar; Entscheidungszustand geht verloren | vollständiges Snapshot-/Payloadmodell; Audit append-only; Round-trip-Contract-Test mit allen Feldern |
| C-06 | P1 – Architekturinkonsistenz | `ImportsController.ApplyResolutions` | Controller lädt, mutiert und speichert; Resolution-Service persistiert nicht | Concurrency-/Transaktionslogik liegt über API und Application verteilt; künftiger Worker müsste sie duplizieren | Application-Use-Case `ResolveImportCommandHandler`/`ImportResolutionService` soll Load–Validate–Apply–Save atomar kapseln |
| C-07 | P1 – Defekt | `ImportCommitService` catch-Block | Zielwrite und Raw-Archivierung sind nicht atomar; nach erfolgreichem Write kann Raw-Fehler Status `Failed` erzeugen; Exception wird ungefiltert weitergeworfen | Retry kann doppelten Write verursachen; API liefert 500 statt stabilen Vertrag | Write-Receipt/Outbox und idempotente Writer; Raw-Archiv getrennt retrybar; definierte Fehlerübersetzung |
| C-08 | P1 – Architekturinkonsistenz | `ExcelImportAnalysisService.AnalyzeAsync` | Infrastructure baut `ImportCoordinator` samt Application-Komponenten manuell | DI-Konfiguration wird umgangen; Tests/Austauschbarkeit und weitere Formate werden schwerer | formatbezogene Reader-Factory/Analysis-Handler injizieren; Use-Case-Orchestrierung in Application belassen |
| C-09 | P1 – Defekt/Tech Debt | `ImportReport.CustomerCount`, `BuildingCount` und `ImportCoordinator` | Counts sind `init`; Coordinator setzt sie nicht erkennbar, obwohl Daten gelesen werden | API/UI können 0 melden, obwohl Daten vorhanden sind | Counts aus Payload ableiten oder beim Erzeugen konsistent setzen; Test mit realer Workbookanalyse |
| C-10 | P1 – Defekt | `ImportFeature.tsx` | Mock-Issues und feste Counts; Analyze/Resolution/Commit sind nur lokale Step-Wechsel | UI meldet erfolgreichen Commit ohne API-Aufruf und ohne WriteGate | Service vollständig anbinden; Serverreport als Source of Truth; Loading/Error/Retry und Statusabgleich implementieren |
| C-11 | P1 – Security | API Requests/Controller | UserId kommt uneinheitlich aus Header (Analyze) bzw. Body (Resolution/Commit) | Spoofing und falscher Audit-Trail; kein Tenant-/Rollenbezug | authentifizierten `ICurrentUser` nutzen; UserId aus Requests entfernen; Policies für Analyze/Resolve/Commit |
| C-12 | P1 – Deployment | `AddImportServices` und dateibasierte Adapter | alle Pfade unter lokalem `ContentRoot/App_Data`; Singleton-Locks gelten nur pro Prozess | Pod-Neustart verliert Zustand, mehrere Replikas divergieren, ReadWriteOnce-Abhängigkeit | DB/Objektstorage konfigurierbar machen; lokale Variante nur Development; stateless API |
| C-13 | P1 – Security/Robustheit | `ImportsController.Analyze`, `ExcelImportAnalysisService` | nur Endung wird geprüft; keine Größenbegrenzung, Signaturprüfung, Malwareprüfung oder Cleanup bei Parsefehler | DoS, Storage-Wachstum und problematische Uploads | Requestlimits, Content-Validierung, Scan-Hook, Quota und Staging-Retention/Finally-Cleanup |
| C-14 | P1 – Architekturinkonsistenz | `Enset.Infrastructure/DBContext.cs`, `Persistence/EnsetDbContext.cs` | zwei gleich benannte DbContexts/Namespaces bzw. konkurrierende Persistenzstrukturen; Konfigurationen werden im gezeigten Context nicht via `ApplyConfigurationsFromAssembly` geladen | falscher Context kann Migration/DI bestimmen; Import-Entity-Konfigurationen möglicherweise wirkungslos | genau einen produktiven Context und Namespace festlegen; Model-Tests und Migration neu verifizieren |
| C-15 | P1 – Konfiguration | `EnsetDbContextFactory.CreateDbContext` | Connection String inklusive `Password=postgres` hart codiert | nur lokale Umgebung, Secret im Code, falsche Migrationen gegen falsches Ziel möglich | Design-Time-Konfiguration aus Environment/User Secrets mit klarer Fehlermeldung |
| C-16 | P1 – API | `ImportsController.Commit`, globale Pipeline | Writer-/IO-Ausnahmen werden nicht fachlich übersetzt; ProblemDetails-Schema nur teilweise deklariert | inkonsistente 500er, NSwag kennt Fehlerverträge unvollständig | zentrale Exception-to-ProblemDetails-Zuordnung; alle ResponseTypes dokumentieren; Correlation ID |
| C-17 | P2 – Dokumentationsinkonsistenz | `README.md`, `docs/01_Architecture.md`, `docs/06_API.md`, `docs/07_Frontend.md` | behaupten teils React/OpenAPI/EF-Reportpersistenz als offen oder nur JSON als vorhanden | Fehlentscheidungen bei Weiterentwicklung | nach Stabilisierung des Arbeitsbaums IST-Doku aktualisieren; Baseline und Review klar trennen |
| C-18 | P2 – Repository Hygiene | versionierte `bin`/`obj`, `.lnk`, lokale Beispieldaten | Buildprodukte und rechnerbezogene Verknüpfung sind getrackt; `.gitignore` wirkt rückwirkend nicht | große Diffs, Merge-Konflikte, Supply-/Datenschutzrisiko | separat inventarisieren und aus Git-Index entfernen; Datenklassifikation vor Entfernen/Verlagern |

## 4. Strukturelle Komplexität

### Notwendige Komplexität

Reader, Mapper, Validator, Duplicate Check, Report, Resolution, WriteGate und Writer schützen unterschiedliche fachliche oder technische Grenzen. Ihre Trennung ist für mehrere Formate, Data Products und Deploymentziele sinnvoll. Ebenso sind API-Verträge getrennt von Application-Modellen, Infrastructure-Persistenzentities getrennt von Workflowmodellen und `api/generated` vom handgeschriebenen Frontend zu halten. Kleine Klassen wie `ImportAuditEntry`, `ImportSourceFileMetadata` oder ein einzelner Writer-Port sind daher nicht allein wegen ihrer Größe zu verschmelzen.

### Unnötige Komplexität

- Leere und vorbereitende Quelldateien erhöhen derzeit das Suchrauschen, sind aber nicht pauschal obsolet. Abschnitt 16 bewertet jeden Platzhalter einzeln; die Kategorien A bis C bleiben ausdrücklich bestehen.
- `BuildingImportService`, `MeterImportService`, `MeterReadingImportService`, `BuildingIndentityKeyBuilder` und `MeterIdentityKeyBuilder` bestehen nur aus TODOs; zwei DuplicateValidatoren werfen `NotImplementedException`.
- `ImportRunner` ist eine reine Durchreichklasse zu `IImportCoordinator`; im aktuellen Konsolenhost schützt sie keine zusätzliche Grenze.
- `ConfiguredExcelImportReader` überlappt mit `ExcelImportReader`.
- `IExcelReader`, `IExcelCustomerReader`, `IExcelWorkbookReader` sowie zwei `ExcelWorkbookWriter`-Konzepte bilden konkurrierende Excel-Pfade.
- `IImportService` unter Infrastructure ist ein Port im falschen Layer und nutzt mit `MeterReadingReaderFactory` ein Service-Locator-Muster über `IServiceProvider`.
- Die Ordnerfolge `Imports/DuplicationCheck/Resolutions` enthält den allgemeinen `ApplyResolutionService`, obwohl Resolution nicht nur technische Dublettenprüfung ist.
- `components/models` im Web versteckt Feature-State unter Präsentationscode; gleichzeitig existieren leere `types`, `services`, `features`, `components`-Platzhalter.

### Qualitätsbewertung

| Kriterium | Befund |
|---|---|
| Essential complexity | Importfreigabe und Provenance rechtfertigen einen expliziten Lifecycle und mehrere Adapter |
| Accidental complexity | hoch in inaktiven Alt-/Platzhalterpfaden und parallelen Persistenz-/Excel-Konzepten |
| Discoverability | Projektgrenzen gut, innerhalb `Imports` durch technische Tiefenstruktur und falsche Ablage mittelmäßig |
| Cohesion | aktive Analyse/Commit-Kerne kohärent; Infrastructure/Imports und DuplicationCheck mischen aktive und zukünftige Features |
| Coupling | statische Referenzrichtung gut; Laufzeitkopplung an Filesystem und mutable `ImportReport` hoch |
| Testability | Ports helfen, aber Zeit, Filesystem, manuelle Konstruktion und konkreter Reportgraph erschweren deterministische Tests |
| Replaceability | Writer/Repository grundsätzlich ersetzbar; EF- und JSON-Vertrag derzeit semantisch nicht austauschbar |
| Future extensibility | gute Grundachsen, aber `Customer`-zentrierter Report verhindert weitere Entitäten ohne Modellerweiterung |

## 5. Interface- und Abstraktionsreview

Bewertung: **A** zwingend beibehalten, **B** sinnvoll aber umbenennen/verschieben, **C** konkrete Vorbereitung ohne aktuelle Nutzung, **D** ohne ausreichenden Nutzen entfernbar.

| Interface | Consumer | Implementierungen | Zweck | Bewertung A-D | Empfehlung |
|---|---|---:|---|:---:|---|
| `IImportReportRepository` | Analysis, Controller, Commit | JSON, EF, Testfake | Persistenzport | A | Consumer-nahe Application-Grenze behalten; Version/Concurrency und Save-Semantik präzisieren |
| `IImportWriter` | `ImportCommitService` | Excel, Database, Console, Testfake | kontrollierte Zieladapter | A | behalten; idempotenten Write-Receipt und Capabilities ergänzen |
| `IRawZoneWriter` | Commit | Filesystem | Raw-Storage-Port | A | behalten; eher `IRawSourceArchive` nennen und unabhängig retrybar machen |
| `IImportWriteGate` | Commit, Tests | eine | zentrale Sicherheitsregel | A | explizit behalten; Zustandsautomat und Payload-Integrität integrieren |
| `IImportAnalysisService` | API | Excel | Host-unabhängiger Use-Case-Port | B | Implementierung aus Infrastructure-orchestriertem Object Graph lösen; als `IAnalyzeImport` im Feature platzieren |
| `IImportCommitService` | API/Runner | eine | Commit-Use-Case | B | als `ICommitImport`/Command Handler näher zum Consumer; Port ist für Hosttests sinnvoll |
| `IApplyResolutionService` | API/Runner | eine | reine Resolutionlogik | B | zu persistierendem Application-Use-Case erweitern oder pure Domain-Funktion ohne Interface; derzeit Name verspricht mehr als Payloadänderung |
| `IImportCoordinator` | Worker/Tests | eine | Analysepipeline | B | `IAnalyzeWorkbook` benennen; nicht zusätzlich zu `IImportAnalysisService` unklar parallel führen |
| `IImportReader` | Coordinator | Excel, Workerwrapper, Testfake | Formatadapter | A | behalten; async/stream-/sourcebezogenen Vertrag statt parameterlosem `Read()` planen |
| `IImportMapper` | Coordinator | Customer mapper, Fake | Customer-Mapping | B | fachlich als `ICustomerImportMapper`; generischen Namen vermeiden |
| `IImportValidator` | Coordinator | Excel validator, Fake | Workbookregeln | B | als `IImportWorkbookValidator`; Formatname aus Implementierung entfernen, wenn Regeln fachlich sind |
| `IDuplicationCheckService` | Coordinator | eine, Fake | Dublettenport | A | behalten, wenn später DB-/Search-Abgleich folgt; `ICustomerDuplicateDetector` bis zur Generalisierung |
| `IImportLogger` | Coordinator | API, Console, Fake | Host-Logging | D | durch `Microsoft.Extensions.Logging.ILogger<T>` ersetzen; eigener dreimethodiger Wrapper verliert strukturierte Logs |
| `IExcelReader` | Worker/WorkbookReader | eine | älterer kombinierter Dateireader | D | mit `IExcelWorkbookReader` konsolidieren; vorher Reader-Contract-Tests |
| `IExcelCustomerReader` | kein aktiver Hauptpfad | eine | Stream-Kundenreader | C | nur behalten, falls unabhängiger Customer-Import konkret geplant; sonst samt Altpfad entfernen |
| `IExcelWorkbookReader` | `ExcelImportReader` | eine | ClosedXML-Abgrenzung innerhalb Infrastructure | B | als interne Infrastructure-Abstraktion belassen oder konkrete Klasse nutzen; gehört nicht in Application |
| `IExcelWorkbookWriter` | `ExcelImportWriter` | eine | technische Workbookmutation | B | intern neben Implementierung; nicht mit Export-`IExcelWriter` parallel benennen |
| `IExcelWriter` | Exportadapter | eine | umfangreicher Excel-Export-/Updatevertrag | C | nur bei aktivem Export-Use-Case behalten; sonst aus produktivem Pfad nehmen |
| `IMeterReadingReader` | Factory/ImportService | CSV | vorbereiteter Zeitreihenreader | C | gute künftige Portidee, aber als eigenes Feature mit Use-Case und Tests aktivieren |
| `IMeterReadingReaderFactory` | `ImportService` | eine | Quellenwahl | D | Service Locator entfernen; `IEnumerable<IMeterReadingReader>` plus `CanRead` oder keyed DI |
| `IMeterReadingMapper` | kein aktiver Importpfad | eine | EF-gebundenes Mapping | C | Port consumer-nah in Application, DB-Lookup separieren; erst bei aktivem Use-Case |
| `IMeterLookupService` | MeterReadingMapper | eine | Datenbanklookup-Port | C | bei Zeitreihenimport sinnvoll; Naming/Consumer nach Application verschieben |
| `IImportValidationService` | kein aktiver Hauptpfad | eine | parallele Customerprüfung | D | in aktiven Validator/Duplicate Check integrieren oder entfernen |
| `IImportService` (Infrastructure) | kein sichtbarer Host | eine | MeterReading-Reader-Aufruf | D | Use-Case-Port gehört nach Application; aktuelle Durchreichimplementierung entfernen |

## 6. Importarchitektur

### Ist-Ablauf

```text
HTTP Upload
  -> ImportsController.Analyze
  -> ExcelImportAnalysisService (stagen + SHA-256)
  -> manuell erzeugter ImportCoordinator
  -> ExcelImportReader -> CustomerImportMapper
  -> ExcelImportValidator -> DuplicationCheckService
  -> ImportDecisionEngine -> ImportReport
  -> JsonImportReportRepository

POST resolutions
  -> Controller lädt Report
  -> ApplyResolutionService mutiert Issues/Status/Audit
  -> Controller speichert Report

POST commit
  -> ImportCommitService lädt Report
  -> ImportWriteGate
  -> Status Committing speichern
  -> gewählter IImportWriter
  -> optional FileSystemRawZoneWriter
  -> Status Committed/Failed speichern
```

Reader, Mapper, Validator und Duplicate Check sind in der aktiven Customer-Analyse sichtbar. Ein eigenständiger normalisierender Schritt existiert nicht; der verbindliche „User Resolution → ApplyResolutionService“-Schritt ist nur auf Issue-Metadaten umgesetzt. Building-Daten werden gelesen und validiert, aber nicht in `ImportReport` als schreibbarer Payload transportiert. Meter/MeterReading haben einen separaten, nicht integrierten CSV-/Servicepfad. Damit bestehen widersprüchliche Entwicklungsachsen, auch wenn nur der Customer-Pfad im API-Flow aktiv ist.

### Statusmodell

`Pending`, `AwaitingResolution`, `ReadyToCommit`, `Committing`, `Committed`, `Failed` werden als frei setzbares Enum auf einem mutablen Report geführt. Statusentscheidungen sind in `ImportCoordinator`, `ApplyResolutionService` und `ImportCommitService` verteilt. Erlaubte Übergänge sind nicht zentral modelliert. `Failed` besitzt keine definierte Resume-Transition; ApplyResolution blockiert nur `Committing` und `Committed`, sodass ein `Failed`-Report wieder auf `ReadyToCommit` gesetzt werden kann. Das kann sinnvoll sein, ist aber aktuell implizit und unauditiert als „Resume“.

Empfohlen wird keine große Framework-State-Machine, sondern eine kleine Application-Komponente mit expliziten Transitionen und Gründen:

```text
Pending -> Analyzing -> AwaitingResolution | ReadyToCommit | AnalysisFailed
AwaitingResolution -> AwaitingResolution | ReadyToCommit
ReadyToCommit -> Committing
Committing -> Committed | CommitFailed
CommitFailed -> ReadyToCommit (expliziter Retry mit neuer AttemptId)
```

### WriteGate, Resolution und Commit

Das Gate ist als obligatorischer Application-Port richtig positioniert und wird im aktiven API-Commit nicht umgangen. Technisch könnten Hosts dennoch jeden konkreten `IImportWriter` direkt aufrufen; das ist am Composition Root nicht vollständig verhinderbar. Architekturtests sollten deshalb verbotene Referenzen aus API/Worker auf Writer-Implementierungen außerhalb der Registrierung prüfen. Entscheidender ist: Das Gate prüft Status und Issueflags, nicht ob ein materialisierter Resolution-Payload existiert oder mit Reportversion und Source Hash übereinstimmt.

`ImportDecisionEngine` und die private `DetermineDecision`-Logik in `ApplyResolutionService` duplizieren dieselbe Verantwortung mit leicht anderer Semantik. Diese Entscheidung und `DetermineStatus` gehören in eine zentrale Policy. `CanWrite` ist nur ein Boolean-Wrapper um `Evaluate` und hat keinen eigenen Nutzen.

### Audit, Parallelität und Idempotenz

Audit-Einträge sind mutable Listenbestandteile. JSON-Dateien und EF-Update löschen/ersetzen den Trail; Unveränderlichkeit ist nicht gegeben. Commit-Audit verwendet für Start, Abschluss und Fehler denselben Command-Timestamp, während `UpdatedAt` teils `UtcNow` nutzt. Ein `TimeProvider` und `AttemptId`, Reportversion, Actor/Tenant, CorrelationId und Writer-Receipt fehlen.

JSON synchronisiert nur innerhalb einer Singleton-Instanz. Mehrere Prozesse/Pods und konkurrierende Controllerrequests sind ungeschützt. Weder SHA-256 noch ImportId dienen als Idempotency Key. Wiederholtes Analyze derselben Datei erzeugt neue Imports; wiederholtes Commit kann nach Crash/Teilfailure erneut schreiben. Für V1.0 sind optimistische Concurrency, eindeutige CommitAttempts, idempotente Writer und persistente Staging-/Raw-Referenzen zwingend.

## 7. API und OpenAPI

Die Routen `/api/v1/imports/analyze`, `/{id}`, `/{id}/resolutions` und `/{id}/commit` sind verständlich und NSwag-fähig. Für einen Workflow sind actionartige Subresources vertretbar. Langfristig wäre `POST /api/v1/imports` für das Anlegen/Analysieren, `PUT/PATCH /{id}/resolutions` und `POST /{id}/commit-attempts` semantisch klarer und idempotenter. URL-Versionierung ist vorhanden, aber nicht durch ein API-Versioning-Paket oder mehrere OpenAPI-Dokumente abgesichert.

Stärken:

- API-spezifische Request/Response-Typen liegen nun in `Enset.Api/Contracts`.
- interne Staging-/Raw-Pfade werden im Mapper nicht veröffentlicht.
- `CancellationToken` wird in allen Controlleraktionen weitergereicht.
- `ProblemDetails` wird für bekannte 400/404/409-Fälle genutzt.
- Swagger und der generierte Client bauen erfolgreich; UI-Komponenten importieren den Client nicht direkt.

Verbesserungen:

- Analyze sollte bei Neuanlage `201 Created` mit Location oder dokumentiert asynchron `202 Accepted` liefern; `200` ist möglich, aber weniger aussagekräftig.
- Body-/Formvalidierung über DataAnnotations/FluentValidation und ein einheitliches ValidationProblemDetails-Schema ergänzen.
- Alle Endpunkte brauchen vollständige `[ProducesResponseType]`-Angaben, besonders 401, 403, 404, 409, 413, 415 und 500.
- `Commit` fängt Infrastrukturfehler nicht; eine globale Exceptionmap muss bekannte Application-Ausnahmen in stabile Problemtypen/URIs übersetzen.
- `DateTime.UtcNow` und UserId gehören nicht in Controllerlogik; `TimeProvider` und authentifizierter Actor-Context verwenden.
- Eine Concurrency-Version als ETag/`If-Match` in GET/Resolution/Commit aufnehmen.
- OpenAPI-Generation soll reproduzierbar aus einem gebauten Artefakt/CI erfolgen; der npm-Script benötigt derzeit eine lokal laufende API auf Port 5000.
- CORS ist nicht konfiguriert. Im Development funktioniert der Vite-Proxy; bei gleichem Kubernetes-Ingress ist CORS unnötig, bei getrennten Origins muss eine enge konfigurierbare Policy existieren.

## 8. Frontendstruktur

Der Schnitt `Pages -> Features -> Components` ist für den aktuellen Umfang passend: `ImportPage` komponiert nur `ImportFeature`, und Wizard-Step-Komponenten sind weitgehend präsentationsorientiert. `services/importService.ts` kapselt den generierten Client korrekt; `api/generated/ensetApiClient.ts` ist klar markiert und wurde nicht manuell verändert.

Aktuelle Abweichungen:

- `ImportFeature` hält Workflow- und Beispieldaten zugleich und führt keinen Serviceaufruf aus.
- `importService` implementiert nur Analyze und ist unbenutzt; Get/Resolution/Commit und konsistente `ApiException`-Übersetzung fehlen.
- `ImportIssueViewModel`, `ImportResolutionAction` und `ImportResolutionSelection` duplizieren generierte Vertragstypen teilweise. UI-spezifische ViewModels sind legitim, brauchen aber explizite Mapper statt identischer Shadow-Enums.
- `components/models` ist die falsche Richtung; Feature-State gehört etwa nach `features/imports/model`.
- `useImportWizard.ts`, `WizardState.ts` und `WizardStep.ts` sind leer; parallel existiert `types/ImportWizardStep.ts`.
- `App.css` und React-Logo sind Vite-Starterreste.
- API Base URL ist `""`; das passt zu Vite-Proxy bzw. Same-Origin-Ingress, ist aber nicht explizit über `VITE_API_BASE_URL` mit sicherem Default und Deploymentdokumentation steuerbar.

Für die reale API-Anbindung ist ein `useReducer` mit diskriminierter Union ausreichend; eine externe State-Library ist derzeit nicht nötig. Zustände sollten Serverstatus und Requeststatus trennen, etwa `idle/uploading/analyzed/resolving/ready/committing/completed/error`, ungültige UI-Transitions verhindern und nach jeder Mutation den Serverreport übernehmen. Bei Navigation/Reload muss die ImportId in URL oder Session stehen und der Report neu geladen werden. Eine formale State-Machine-Library erst einsetzen, wenn parallele Uploads, Background Jobs oder komplexe Retry-Zweige tatsächlich entstehen.

## 9. Konfiguration und Deployment

| Bereich | Befund | Einordnung |
|---|---|---|
| Development | API Port 5000 und Vite-Proxy stimmen überein; Swagger nur Development | brauchbar lokal |
| Appsettings | keine sichtbaren `appsettings*.json` für Storage, DB, Limits, CORS oder Logging | Defekt für konfigurierbaren Betrieb |
| Launch profiles | nur `src/Enset.Api/Properties/launchSettings.json` ist wirksam; offene Tabs deuten auf zusätzliche/alte Pfade, im Scan nicht als aktive Konfiguration belegt | bereinigen/dokumentieren |
| Worker | absoluter Pfad zu `C:\Users\rdpadmin\...\Externe Daten\...xlsm` | funktioniert nur auf diesem Windows-Rechner |
| EF Design Time | localhost-Credentials im Code | lokal und unsicher |
| Filesystem | Reports, Staging, Raw und Outputs unter API-ContentRoot | nicht stateless, podlokal |
| Docker/Kubernetes | keine Dockerfiles, Compose-, Helm- oder Kubernetes-Manifeste | nicht nachgewiesen |
| Health | keine Health Checks, Readiness oder Liveness | fehlt |
| Logging | eigener Console-Wrapper; keine strukturierte Konfiguration, Traces oder Metrics | nicht betriebsreif |
| Secrets | keine Secret-Provider-Konfiguration; DB-Passwort im Factorycode | kritisch vor Deployment |
| CORS/Ingress | Development-Proxy vorhanden, Produktionsrouting nicht dokumentiert | vor V1.0 klären |

Empfohlene Betriebsgrenze: Web als statische Assets/CDN oder eigener Container, API stateless, Worker als separater Hosted Service/Deployment, PostgreSQL für Workflow-/Curated-Zustand und Object Storage/PVC nur hinter einem Storage-Port. Readiness muss DB/erforderlichen Storage prüfen, Liveness nur Prozessgesundheit. OpenTelemetry für HTTP, ImportAttempt, Queue und Writer-Latenz sowie strukturierte Logs mit ImportId/CorrelationId sind vor horizontaler Skalierung sinnvoll.

## 10. Testabdeckung

Vorhanden sind sieben xUnit-Tests in einer Datei: analyse-only Coordinator, JSON-Roundtrip, Resolutionänderung/Audit, zwei Gate-Fälle, Writeraufruf nach Gate und Controller ohne direkte Writerabhängigkeit. Das ist ein guter Anfang für Kernregeln, aber keine vollständige Testpyramide. Aktuell kompiliert das Projekt wegen `ImportCommitRequest` nicht.

Ausgeführter Nachweis am 19.07.2026:

| Befehl | Ergebnis |
|---|---|
| `dotnet build src/Enset.Api/Enset.Api.csproj --no-restore` | erfolgreich, 0 Warnungen, 0 Fehler |
| `dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore` | fehlgeschlagen: CS0246 in `DuplicationResolutionRunner.cs:17` |
| `dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj --no-restore` | fehlgeschlagen: CS0246 in `ImportArchitectureTests.cs:226` |
| `npm run build` | erfolgreich |
| `npm run lint` | erfolgreich |

Kritisch fehlende Tests, in Prioritätsreihenfolge:

1. Resolution-Actions verändern den tatsächlich an Writer übergebenen Payload korrekt; ungelöste/inkonsistente Actions blockieren.
2. vollständige Statusübergangsmatrix einschließlich Failed/Retry und verbotener Transitionen.
3. zwei parallele Resolution-/Commitrequests; nur ein Commit darf gewinnen.
4. Idempotency: Retry nach Timeout, Crash nach Zielwrite und Raw-Archivfehler erzeugen keinen doppelten fachlichen Write.
5. EF-Repository-Roundtrip aller Reportfelder und Update/Concurrency; append-only Audit.
6. echte `.xlsx`/`.xlsm` Reader-/Writer-Fixtures inklusive fehlerhafter und großer Dateien.
7. HTTP-Integrationstests über `WebApplicationFactory` für Statuscodes, ProblemDetails, Uploadlimits, Auth und ETags.
8. OpenAPI-Snapshot plus reproduzierbare NSwag-Generierung und TypeScript-Compile als Contract Test.
9. React Component-/Reducer-Tests für ungültige Navigation, API-Fehler und Reload; danach ein E2E-Happy-Path und Konfliktpfad.
10. Architekturtests für Projektabhängigkeiten, ClosedXML/EF nur in Infrastructure, kein generierter Client außerhalb Serviceadapter und kein Writer im Controller/Coordinator.

Empfohlene Pyramide: viele pure Application-Policy-/Mappertests; weniger Adapterintegrationstests mit realen Exceldateien/PostgreSQL; wenige HTTP-/Browser-E2E-Tests. Das aktuelle gemeinsame Testprojekt sollte mittelfristig in `Application.UnitTests`, `Infrastructure.IntegrationTests`, `Api.IntegrationTests` und Webtests getrennt werden, sobald deren Fixtures tatsächlich verschieden sind.

## 11. Zielstruktur

Die bestehenden Projekte bleiben erhalten. Innerhalb von Application und Web wird fachlich nach Feature/Lifecycle statt primär nach technischen Typen geschnitten:

```text
src/
├── Enset.Domain/
│   ├── Customers/
│   ├── Buildings/
│   ├── Energy/
│   ├── Documents/
│   └── DataProducts/                 # erst bei konkretem Vertrag
├── Enset.Application/
│   ├── Imports/
│   │   ├── Analyze/
│   │   │   ├── AnalyzeImport.cs
│   │   │   ├── ImportAnalysisResult.cs
│   │   │   └── Ports/                # Reader, Mapper, Validator, duplicate detector
│   │   ├── Resolve/
│   │   │   ├── ResolveImport.cs
│   │   │   ├── ResolutionPolicy.cs
│   │   │   └── ResolvedImportPayload.cs
│   │   ├── Commit/
│   │   │   ├── CommitImport.cs
│   │   │   ├── ImportWriteGate.cs
│   │   │   └── Ports/                # Writer, raw archive
│   │   ├── Lifecycle/
│   │   │   ├── ImportReport.cs
│   │   │   ├── ImportIssue.cs
│   │   │   ├── ImportStatusPolicy.cs
│   │   │   └── ImportAuditEntry.cs
│   │   └── Persistence/
│   │       └── IImportReportRepository.cs
│   └── DataProducts/                 # später separate Publish-Use-Cases
├── Enset.Infrastructure/
│   ├── Imports/
│   │   ├── Excel/
│   │   ├── Csv/
│   │   ├── Persistence/
│   │   │   ├── Ef/
│   │   │   └── Json/                 # nur Development
│   │   ├── RawStorage/
│   │   └── Writers/
│   └── Persistence/
│       ├── EnsetDbContext.cs
│       ├── Configurations/
│       └── Migrations/
├── Enset.Api/
│   ├── Imports/
│   │   ├── ImportsController.cs
│   │   ├── Contracts/
│   │   └── ImportContractMapper.cs
│   ├── Errors/
│   └── Composition/
├── Enset.Worker/
│   ├── Program.cs
│   ├── ImportWorker.cs
│   └── Composition/
└── Enset.Web/
    └── src/
        ├── app/                       # router, layout, providers
        ├── features/imports/
        │   ├── api/                   # handgeschriebener Adapter um generated
        │   ├── model/                 # reducer, state, mapper
        │   ├── components/
        │   └── ImportPageContent.tsx
        ├── shared/ui/
        └── api/generated/             # ausschließlich generiert
tests/
├── Enset.Application.UnitTests/
├── Enset.Infrastructure.IntegrationTests/
├── Enset.Api.IntegrationTests/
└── Enset.Web.Tests/
```

Data Platform, Data Lake House, Data Space und EMS sollen nicht vorsorglich als vier leere Projekte angelegt werden. Erst konkrete Verträge erhalten eigene Module/Deployables. Standardisierte Data Products gehören hinter versionierte Application-/Integration-Ports und niemals als direkter Zugriff von Business Modules auf interne Tabellen.

## 12. Konsolidierungskandidaten

| Dateien/Klassen | Aktueller Zweck | Vorschlag | Vorteil | Nachteil | Empfehlung |
|---|---|---|---|---|---|
| `ImportRunner` + Worker `Program` | Runner delegiert nur an Coordinator | Runner entfernen oder mit echtem `BackgroundService` ersetzen | weniger Durchreichcode | späterer Host-Hook entfällt | jetzt entfernen ist sicher, da keine Architekturgrenze; vorher Worker-CLI-Smoke-Test |
| `ConfiguredExcelImportReader` + `ExcelImportReader` | beide binden Datei an Workbookreader | einen Adapter behalten | eindeutiger Readerpfad | mögliche alte Aufrufer migrieren | sicher bei Usage-Suche; Application-Port bleibt; Reader-Fixturetests vorher |
| `IExcelReader` + `IExcelWorkbookReader` + `IExcelCustomerReader` | überlappende Excel-Leseverträge | aktiven Workbook-Port vereinheitlichen, Spezialreader nur bei Use-Case | bessere Discoverability | weniger vorbereitete Austauschpunkte | Infrastructure/Application-Grenze bleibt über `IImportReader`; Customer-/Building-Parsingtests vorher |
| Import- und Export-`ExcelWorkbookWriter` | technische Workbookmutation vs. breiter Export | eindeutig `ImportWorkbookUpdater` und `ExcelExportWriter`; nicht blind verschmelzen | Naming ohne falsche Gleichheit | zwei kleine Adapter bleiben | keine Zusammenlegung, nur Abgrenzung; Writer-Regressionstests |
| `ImportDecisionEngine` + `ApplyResolutionService.DetermineDecision/Status` | doppelte Status-/Decision-Policy | `ImportReadinessPolicy` zentralisieren | konsistente Regeln | zentrale Klasse wird kritischer | empfohlen; Grenze bleibt Application; vollständige Transitiontests vorher |
| `ApiProblems`-Methoden | wiederholte ProblemDetails-Erzeugung | kleine Factory behalten oder zentraler Exception Handler | stabile Fehlerverträge | Controller verliert lokale Sichtbarkeit | erst nach Exceptiontaxonomy; HTTP-Contract-Tests vorher |
| `JsonImportReportRepository` + EF-Repository | gleiche Portabsicht, andere Semantik | nicht zusammenlegen; Portvertrag korrigieren, JSON nur Development | echte Austauschbarkeit | Migrationsaufwand | beide Adaptergrenzen erhalten; Repository-Contract-Suite vorher |
| ausschließlich Kategorie-D-Platzhalter aus Abschnitt 16 | nachweislich überholte oder doppelte Vorbereitungen | erst nach referenzfreiem Nachweis entfernen | weniger Scheinfunktionalität | historische Absicht geht verloren | nur die einzeln als D bewerteten Dateien; Build/rg und Architekturtest vorher |
| Web `ImportWizardStep`/`WizardStep` und leere State/Hook-Dateien | parallele Typvorbereitung | einen Reducer-State im Featuremodell | klare State Source | initial etwas mehr zusammenhängender Code | empfohlen; Komponenten-/Reducertests vorher |
| `CanWrite` + `Evaluate` | Boolean-Weiterleitung | nur strukturiertes `Evaluate`/`EnsureAllowed` behalten | kein Verlust von Fehlerdetails | minimale API-Änderung | D-Kandidat; Gategrenze bleibt; Gate-Tests vorher |

Keine Zusammenlegung ist allein wegen kleiner Dateien empfohlen. Entscheidend sind identische Verantwortung, identischer Änderungsgrund und Erhalt der Adapter-/Use-Case-Grenze.

## 13. Refactoring-Roadmap

### Phase A – Sofortige Korrekturen

| Maßnahme | Priorität | Aufwand | Risiko | Projekte | Notwendige Tests | Abhängigkeiten |
|---|---:|:---:|---|---|---|---|
| `ImportCommitCommand`-Umbenennung in Worker/Tests abschließen | P0 | XS | niedrig | Worker, Tests | Build/Test | keine |
| Resolution in echten resolved Payload überführen und Gate daran binden | P0 | L | hoch | Application, Infrastructure, Tests | Action-, Payload-, Writer-Tests | Statuspolicy festlegen |
| EF-Repository-Vertrag und vollständigen Roundtrip reparieren oder bis dahin bewusst nicht registrierbar markieren | P0 | M | mittel | Application, Infrastructure | Repository-Contract | Payloadmodell |
| atomare Concurrency-Transition für Resolution/Commit definieren | P0 | L | hoch | Application, Infrastructure, API | Parallelitäts-/ETag-Tests | produktive Persistenz |
| UI-Erfolg ohne API-Aufruf verhindern | P1 | M | mittel | Web | Reducer/Service/E2E | stabiler API-Vertrag |
| hart codierte Credentials und Worker-Pfad entfernen | P1 | S | niedrig | Infrastructure, Worker | Config-Smoke-Tests | Konfigurationsschema |

### Phase B – Vereinfachung ohne Verhaltensänderung

| Maßnahme | Priorität | Aufwand | Risiko | Projekte | Notwendige Tests | Abhängigkeiten |
|---|---:|:---:|---|---|---|---|
| Platzhalter gemäß Abschnitt 16 kennzeichnen/korrekt ablegen; nur Kategorie D entfernen | P2 | S | niedrig | Application, Infrastructure, Web | Builds, Usage-Scan | Phase A Compilefix |
| Excel-Reader-/Writer-Namen und Altpfade konsolidieren | P2 | M | mittel | Application, Infrastructure, Worker | Excel-Fixtures | aktive Pfade inventarisieren |
| Feature-orientierte Importordner herstellen; Resolution aus `DuplicationCheck` lösen | P2 | M | mittel | Application, Api, Web | Namespace-/Architekturtests | Phase A Modelle stabil |
| eigenen `IImportLogger` durch `ILogger<T>` ersetzen | P2 | S | niedrig | Application, API, Worker | Logging-Smoke-Test | Host-DI |
| einen DbContext/Namespace und eine Configuration-Anwendung festlegen | P1 | M | mittel | Infrastructure | EF Model-/Migrationtest | Repositoryentscheidung |
| versionierte Buildartefakte und Starterreste separat bereinigen | P2 | S | niedrig | Repository, Web | Clean checkout build | Datenklassifikation |

### Phase C – Stabilisierung für Version 1.0

| Maßnahme | Priorität | Aufwand | Risiko | Projekte | Notwendige Tests | Abhängigkeiten |
|---|---:|:---:|---|---|---|---|
| PostgreSQL-Report-/Payloadpersistenz mit Optimistic Concurrency und append-only Audit | P0 | XL | hoch | Infrastructure, Application | DB/Concurrency/Migration | Phase A Vertragsmodell |
| authentifizierter Actor, Rollen und ggf. Tenant-Kontext | P0 | L | hoch | API, Application, Web | Security-/Policytests | Identity Provider |
| DatabaseWriter transaktional für Customer/Building/Meter/MeterReading implementieren | P0 | XL | hoch | Infrastructure, Domain | Mapping, FK, Transaction, E2E | resolved Payload, DB-Schema |
| stabile API-Fehlertaxonomie, ETags, Limits und OpenAPI-Contract-CI | P1 | L | mittel | API, Web, Tests | HTTP/NSwag | Auth, Concurrency |
| Import-History, Resume und idempotente CommitAttempts | P1 | L | hoch | Application, Infrastructure | Crash-/Retrytests | Writer receipts |
| Frontend vollständig anbinden, reloadfähig und fehlertolerant machen | P1 | L | mittel | Web | Component/E2E | stabiler API-Client |

### Phase D – Vorbereitung auf Skalierung

| Maßnahme | Priorität | Aufwand | Risiko | Projekte | Notwendige Tests | Abhängigkeiten |
|---|---:|:---:|---|---|---|---|
| Container für API/Web/Worker plus Compose-Entwicklung | P2 | M | niedrig | Hosts, Ops | Container-Smoke | externe Konfiguration |
| Kubernetes Deployment/Service/Ingress, Probes, Ressourcen und Secret-Referenzen | P2 | L | mittel | Ops | Deployment-/Probe-Test | Container, Health |
| Queue-basierter Worker mit Outbox/Inbox und Lease | P2 | XL | hoch | Worker, Application, Infrastructure | Delivery/Retry/Poison | CommitAttempts, Brokerentscheidung |
| Object Storage für Staging/Raw mit Retention/Immutability | P2 | L | mittel | Infrastructure | Hash/Retention/Access | Storageprovider |
| OpenTelemetry Logs/Metrics/Traces und SLOs | P2 | M | niedrig | alle Hosts | Telemetry-Smoke | Correlation/Attempt IDs |
| Mandantenfähigkeit | P3 | XL | sehr hoch | alle | Isolation/Security/Migration | nur nach konkretem Mandantenmodell |
| versionierte Data-Product-Publisher und Integration Layer | P3 | XL | hoch | neue fachliche Module | Contract/Consumer tests | konkrete Business-Module |

Weitere Formate können nach Stabilisierung über formatbezogene Reader/Parser ergänzt werden. PostgreSQL ist für Stammdaten/Workflow geeignet, TimescaleDB für MeterReading; die Wahl rechtfertigt keinen gemeinsamen Universal-Writer. Message Broker, Mandantenfähigkeit und separate Data-Product-Deployables sollen bewusst erst bei konkretem Bedarf umgesetzt werden.

## 14. Nicht empfohlene Änderungen

- Domain, Application und Infrastructure zu einem Projekt zusammenlegen: reduziert Dateien, zerstört aber die nachgewiesene Abhängigkeitsrichtung.
- Reader, Validator, Duplicate Check, Resolution, Gate und Writer in einen „ImportService“ verschmelzen: würde die zentrale Vorab-Schreibsperre und Testbarkeit schwächen.
- API-Verträge wieder nach Application verschieben: HTTP/Form/ProblemDetails sind Adapterbelange.
- `ImportIssue` und `ImportReport` durch anonyme Dictionaries oder generische Result-Wrapper ersetzen: Status, Audit und Resolution sind wesentliche Fachkomplexität.
- JSON- und EF-Entitymodelle vereinheitlichen, indem EF-Attribute in Application/Domain wandern: Persistenzdetails würden nach innen lecken.
- den generierten NSwag-Client manuell vereinfachen oder UI-Komponenten direkt daran koppeln: Regeneration und Fehlerbehandlung würden instabil.
- für jede künftige Entity sofort eigene Services, Repositories und Projekte erzeugen: die vorhandenen leeren Klassen zeigen bereits die Kosten spekulativer Abstraktion.
- API und Web gemeinsam deployen müssen: Same-Origin kann per Ingress erreicht werden, ohne unabhängige Artefakte aufzugeben.
- jetzt einen Message Broker, Event Sourcing, Saga-Framework oder eine externe Frontend-State-Library einführen: Concurrency und Persistenzverträge müssen zuerst lokal korrekt sein.
- Console-Resolution als zweiten fachlichen Workflow weiterentwickeln: Worker/CLI soll denselben Application-Use-Case verwenden, nicht konkurrierende Regeln.

## 15. Abschließende Bewertung

| Kriterium | Bewertung | Begründung |
|---|:---:|---|
| Schichtentrennung | 7/10 | Projektgraph und Adapterrichtung stimmen; Analysis-Service-Komposition, Altports und zwei DbContexts verwischen Details |
| fachliche Kohäsion | 5/10 | aktive Importkerne sind erkennbar, Resolution liegt unter DuplicationCheck und verändert den Payload nicht |
| Verständlichkeit | 5/10 | gute Host-/Projektgrenzen, aber leere Dateien, Altpfade und widersprüchliche Dokumentation erzeugen Suchaufwand |
| Testbarkeit | 5/10 | Ports und Kernfakes sind vorhanden; Tests kompilieren aktuell nicht, Integration/Concurrency fehlen |
| Erweiterbarkeit | 6/10 | Reader/Writer-Ports helfen; Customer-zentrierter Report und unklare Persistenzsemantik begrenzen neue Entitäten/Formate |
| API-Reife | 5/10 | versionierte Routen, OpenAPI, NSwag, CancellationToken und ProblemDetails vorhanden; Auth, Concurrency und Fehlervertrag fehlen |
| Deployment-Reife | 2/10 | lokale Builds funktionieren, aber Konfiguration, Secrets, Health, Container und stateless Storage fehlen |
| Kubernetes-Eignung | 2/10 | unabhängige Hosts sind eine gute Basis; lokales Filesystem und fehlende Probes/Manifeste verhindern Skalierung |
| Verhältnis Struktur/Komplexität | 5/10 | wesentliche Trennungen sind angemessen, spekulative/tote Klassen und parallele Konzepte erhöhen accidental complexity |

**Klare Empfehlung: B – einzelne Module gezielt restrukturieren.** Die Architektur ist grundsätzlich erhaltenswert. Sofort zu korrigieren sind Compiledefekte, Resolution-Payload, Persistenzvertrag und Concurrency. Vor Version 1.0 folgen Auth, transaktionaler DatabaseWriter, vollständige Contract-/Integrationstests und externe Konfiguration. Docker/Kubernetes, Messaging, Data Products und Mandantenfähigkeit werden anschließend entlang konkreter Betriebs- und Business-Anforderungen vorbereitet, nicht spekulativ vorgebaut.

## 16. Phase-3-Readiness und MVP-Zielmodell

### 16.1 Entscheidung

**Conditional GO für Phase 3, aber NO-GO für einen unmittelbaren fachlichen Write durch `DatabaseImportWriter`.**

Das Repository ist ausreichend strukturiert, um Phase 3 innerhalb der bestehenden Projekte zu beginnen. Die Trennung `ImportCoordinator -> ImportAnalysisService -> ApplyResolutionService -> ImportCommitService -> ImportWriteGate -> IImportWriter -> DatabaseImportWriter` bleibt verbindlich. Der vorhandene Stand reicht jedoch nicht aus, um lediglich PostgreSQL zu registrieren, `EfImportReportRepository` zu aktivieren und Customer-Daten zu schreiben. Ein solcher Direktstart würde die in C-01, C-03, C-04 und C-05 beschriebenen Defekte produktiv machen.

Phase 3 muss deshalb mit einem kurzen relationalen Foundation-Slice beginnen. Erst danach folgt der erste funktionsfähige Customer-`DatabaseImportWriter`-Slice. Diese Einordnung ist keine neue Architekturversion, sondern präzisiert die Umsetzung der bereits in V1.2 festgestellten Persistenz-, Payload- und Concurrency-Lücken. Eine V1.3 ist mangels neuem Implementierungsstand nicht erforderlich.

### 16.2 Abgleich mit dem verbindlichen MVP-Schema

| MVP-Tabelle/Storage | Aktueller Stand | Lücke vor funktionsfähigem Vertical Slice | Phase-3-Einstufung |
|---|---|---|---|
| `import_reports` | `ImportReportEntity` und Migration `ImportReports` vorhanden | Namingkonvention, Version/ConcurrencyToken, Decision, Fehlergrund und Statusübergänge fehlen | zuerst vervollständigen |
| `import_source_files` | Source-Felder sind in `ImportReports` eingebettet | eigenes Entity, Storage-Key statt lokaler Pfade, Hash/Length/MediaType und FK fehlen | zuerst ergänzen |
| `import_issues` | `ImportIssueEntity`/`ImportIssues` vorhanden | aktuelle Resolutionfelder vermischen Befund und Entscheidung; Enum-/Constraint-Strategie fehlt | weiterverwenden und normalisieren |
| `import_issue_resolutions` | nicht vorhanden; nur aktueller Zustand am Issue plus Audittext | jede Benutzerentscheidung als eigene unveränderliche Zeile mit Actor, Timestamp und optionalem Custom Value | zuerst ergänzen |
| `import_payload_snapshots` | nicht vorhanden; JSON speichert `Customers`, EF-Mapping verwirft sie | versionierter, unveränderlicher Analyse-/Resolved-Payload inklusive SchemaVersion und Hash | zwingend vor Writer |
| `import_commit_attempts` | nicht vorhanden; Commitstatus/Audit direkt am Report | AttemptId, IdempotencyKey, Writer/Mode, Start/Ende, Status, Fehler und Write-Receipt | zwingend vor Writer |
| `customers` | Domain-DbSet/Migration vorhanden | fachliche Upsert-Identität ist unklar; `ExternalCustomerId` ist nicht als belastbarer DB-Identifier modelliert | erster Domain-Slice nach Foundation |
| `projects` | vorhanden | für Customer-only-Slice nicht zwingend zu schreiben; FK-/Ownership-Regeln vor späterem Slice definieren | bestehen lassen |
| `buildings` | vorhanden | Building-Payload fehlt im Report; Mappingpfad noch Platzhalter | späterer Vertical Slice |
| `energy_systems` | vorhanden | kein aktiver Importpayload/-mapper | späterer Vertical Slice |
| `meters` | vorhanden, `MeterNumber` unique | Normalisierung und External-Identifier-Regel fehlen | späterer Vertical Slice |
| `meter_readings` | vorhanden, Composite Key | Batch-/Upsert-/Timescale- und Provenance-Strategie fehlen | separater Zeitreihen-Slice |
| `documents` | vorhanden | kein aktiver Importpfad | späterer Slice |
| `external_identifiers` | nicht vorhanden | generische externe Identität mit SourceSystem, EntityType, ExternalId und Unique Constraint fehlt | für Customer-Upsert vorab entscheiden; MVP-Tabelle anlegen, wenn externe ID die Match-Grenze ist |
| `audit_entries` | `ImportAuditEntryEntity`/`ImportAuditEntries` vorhanden | derzeit Report-Child, durch Update ersetzbar und Cascade-löschbar; nicht append-only | vor produktivem Commit härten |
| `imports/{importId}/original-file` | dateibasierte Raw Zone mit Hash im Dateinamen | Archivierung erfolgt nach Zielwrite, lokaler Pfad wird gespeichert; Object-Storage-Key, Immutability und Vorabarchivierung fehlen | Storage-Port beibehalten; Original vor Domain-Commit sicher referenzieren |

Die aktuelle Migration bildet damit nur drei der sechs Workflowtabellen näherungsweise und die Audit-Tabelle prototypisch ab. Sie ist kein ausreichendes relationales Phase-3-Zielmodell. Bestehende Migrationen sollen nicht still umgeschrieben werden, wenn sie bereits irgendwo angewandt wurden; stattdessen ist eine nachvollziehbare Folgemigration bzw. bei garantiert unpubliziertem Prototypschema eine ausdrücklich dokumentierte Baseline-Neuerstellung zu wählen.

### 16.3 Verbindliche Reihenfolge für Phase 3

1. **Buildbaseline reparieren:** `ImportCommitRequest` vollständig auf `ImportCommitCommand` umstellen; Worker und Tests müssen grün sein.
2. **Einen DbContext festlegen:** `Enset.Infrastructure.Persistence.EnsetDbContext` ist wegen `ApplyConfigurationsFromAssembly` der geeignete Kandidat. `src/Enset.Infrastructure/DBContext.cs` darf nicht parallel produktiv bleiben.
3. **Persistenzvertrag festlegen:** `IImportReportRepository.SaveAsync` muss Insert und versioniertes Update eindeutig unterscheiden oder ein atomisches `Save(expectedVersion)` definieren. JSON- und EF-Adapter müssen dieselbe Contract-Test-Suite bestehen.
4. **Workflow-Schema vervollständigen:** SourceFile, IssueResolution, PayloadSnapshot, CommitAttempt und append-only Audit relational modellieren; snake_case über eine konsistente EF-Namingstrategie abbilden.
5. **Resolved Payload materialisieren:** `ApplyResolutionService` muss aus Analyse-Snapshot plus Entscheidungen einen unveränderlichen resolved Snapshot erzeugen. Issueflags allein sind keine Schreibgrundlage.
6. **Commit atomar reservieren:** `ReadyToCommit -> Committing` mit erwarteter Reportversion und eindeutiger AttemptId in PostgreSQL vollziehen. Ein paralleler Request muss verlieren.
7. **Customer-Vertical-Slice implementieren:** `DatabaseImportWriter` konsumiert ausschließlich den freigegebenen Snapshot, schreibt Customer plus External Identifier in einer DB-Transaktion und persistiert ein Write-Receipt.
8. **Commit finalisieren:** Attempt und Report transaktional auf Erfolg setzen; Fehlerzustände unterscheiden „vor Write“, „rollback“ und „Write möglicherweise erfolgt“.
9. **Raw Source absichern:** Originaldatei spätestens vor dem Domain-Write unter dem stabilen Object-Storage-Key referenzierbar machen; ein Raw-Fehler darf keinen bereits erfolgreichen Domain-Write als pauschal `Failed` maskieren.
10. **API/Frontend erst danach umschalten:** EF-Repository über Konfiguration aktivieren; JSON bleibt ausschließlich Development/Test-Fallback.

### 16.4 Minimaler Customer-Writer-Vertrag

Der erste `DatabaseImportWriter` soll bewusst nur Customers unterstützen. Ein erfolgreicher Commit muss atomar nachweisen:

- Reportversion und Status waren beim Reservieren gültig;
- alle erforderlichen Issues besitzen eine persistierte Resolution;
- der verwendete resolved Payload Snapshot ist per ID, Version und Hash eindeutig;
- die externe Customer-Identität ist normalisiert und per Unique Constraint abgesichert;
- `Upsert` und `Replace` haben getrennte, dokumentierte Semantik; für den ersten Slice ist `Replace` besser zu blockieren als unvollständig umzusetzen;
- Domainwrite und CommitAttempt-Receipt laufen in einer Transaktion;
- ein Retry derselben Attempt-/Idempotency-ID erzeugt keine zweite Änderung;
- Writer schreibt weder aus `ImportIssue.FirstValue/SecondValue` noch aus dem mutablen Live-Report, sondern ausschließlich aus dem freigegebenen Snapshot.

Gebäude, Projekte, Meter, MeterReadings, Documents und EnergySystems bleiben im Modell bestehen, werden aber nicht vorgetäuscht als Bestandteil dieses ersten Slices. Jeder weitere Entitytyp erhält anschließend einen eigenen Vertical Slice mit Mapping-, Validierungs-, FK-, Upsert- und Integrationstests.

### 16.5 Entry Criteria und Exit Criteria

**Phase 3 darf begonnen werden, wenn:** die bestehende Projektarchitektur unverändert bleibt, das obige Foundation-Paket als Teil von Phase 3 akzeptiert ist und keine Aktivierung des unvollständigen EF-/DatabaseWriter-Pfads vor dessen Tests erfolgt.

**Der erste Phase-3-Vertical-Slice gilt erst als fertig, wenn:** API-, Worker- und Testbuild grün sind; Migrationen gegen eine echte PostgreSQL-Testinstanz laufen; Repository-Roundtrip und Concurrency getestet sind; Customer-Resolution den Snapshot tatsächlich verändert; ein erfolgreicher Commit Customer/ExternalIdentifier/Attempt/Audit atomar persistiert; Doppelcommit blockiert oder idempotent beantwortet wird; und keine lokale Windows-Pfadabhängigkeit für den DB-Pfad besteht.

## 17. Einzelprüfung der Platzhalter

Leere Dateien besitzen ohne Namespace technisch noch keine Schichtverletzung. Die erwarteten Namespaces ergeben sich aus Ordner und Nachbarcode und sind deshalb bei vollständig leeren Dateien als **Vermutung** markiert. Kategorien A bis C bleiben bestehen; nur D ist entfernbar.

| Datei | Namespace | Aktueller Zweck | Erwartete zukünftige Nutzung | Kat. | Empfehlung |
|---|---|---|---|:---:|---|
| `Imports/AutoFix/ExcelAutoFixEngine.cs` | Vermutung: `Enset.Application.Imports.AutoFix` | leerer AutoFix-Platzhalter | Vorschläge für explizit bestätigte Korrekturen | B | behalten, aber vor Implementierung formatneutral als Resolution-/Proposal-Komponente benennen; niemals automatische Writes |
| `Imports/Normalizer/MeterReadingNormalizer.cs` | Vermutung: `Enset.Application.Imports.Normalizer` | leer | Einheiten-, Timestamp- und Identifier-Normalisierung vor MeterReading-Validation | A | für späteren MeterReading-Slice behalten; pure, deterministische Regeln vorsehen |
| `Imports/Validation/CustomerValidator.cs` | Vermutung: `Enset.Application.Imports.Validation` | leer | Customer-spezifische Regeln im ersten DB-Slice | A | behalten und in Phase 3 gegen resolved Customer DTO implementieren |
| `Imports/Validation/BuildingValidator.cs` | Vermutung: gleicher Namespace | leer | Building-Feld-/Referenzregeln | A | für Building-Slice behalten; nicht in Customer-Slice vortäuschen |
| `Imports/Validation/MeterValidator.cs` | Vermutung: gleicher Namespace | leer | MeterNumber/Unit/Building-Regeln | A | für Meter-Slice behalten |
| `Infrastructure/Imports/Excel/ExcelBuildingReader.cs` | Vermutung: `Enset.Infrastructure.Imports.Excel` | leer | formatbezogenes Building-Parsing | A | behalten; erst aktivieren, wenn klar ist, ob `ExcelWorkbookReader.ReadBuildings` extrahiert oder delegiert wird |
| `Infrastructure/Imports/Mappings/CustomerMapper.cs` | Vermutung: `Enset.Infrastructure.Imports.Mappings` | leer | Import DTO -> Domain/EF Customer | B | Verantwortung ist für Phase 3 nötig, aber Mapping sollte vom `DatabaseImportWriter` genutzt und eindeutig `CustomerPersistenceMapper` genannt werden |
| `Infrastructure/Imports/Mappings/BuildingMapper.cs` | Vermutung: gleicher Namespace | leer | Building DTO -> Domain/EF | B | für später behalten und analog eindeutig benennen |
| `Infrastructure/Imports/Mappings/MeterMapper.cs` | Vermutung: gleicher Namespace | leer | Meter DTO -> Domain/EF | B | für später behalten und analog eindeutig benennen |
| `DuplicationCheck/Identity/BuildingIndentityKeyBuilder.cs` | kein deklarierter Namespace; Ordner deutet auf `...DuplicationCheck.Identity` | TODO; zudem Tippfehler `Indentity` | normalisierter Building-Match-Key | B | behalten, Datei/Klasse vor Implementierung zu `BuildingIdentityKeyBuilder` korrigieren |
| `DuplicationCheck/Identity/MeterIdentityKeyBuilder.cs` | kein deklarierter Namespace | TODO | normalisierter Meter-Match-Key | A | für Meter-Slice behalten |
| `DuplicationCheck/Validation/BuildingDuplicateValidator.cs` | `Enset.Application.Imports.DuplicationCheck.Validation` | Vertrag vorhanden, wirft `NotImplementedException` | Building-Dubletten erkennen | A | behalten; bis Implementierung nicht registrieren; Tests vor Aktivierung |
| `DuplicationCheck/Validation/MeterDuplicateValidator.cs` | gleicher Namespace | Vertrag vorhanden, wirft `NotImplementedException` | Meter-Dubletten erkennen | A | behalten; bis Implementierung nicht registrieren |
| `DuplicationCheck/Services/BuildingImportService.cs` | kein deklarierter Namespace | TODO | unklarer künftiger Building-Use-Case | B | behalten nur als Backlogmarker; später aus `DuplicationCheck/Services` in featurebezogenen Analyze/Commit-Slice verschieben und konkret benennen |
| `DuplicationCheck/Services/MeterImportService.cs` | kein deklarierter Namespace | TODO | unklarer Meter-Use-Case | B | gleiche Behandlung; DuplicationCheck ist nicht Eigentümer des gesamten Imports |
| `DuplicationCheck/Services/MeterReadingImportService.cs` | kein deklarierter Namespace | TODO | Zeitreihenimport | B | später eigener MeterReading-Vertical-Slice; nicht unter DuplicationCheck |
| `DuplicationCheck/Services/CustomerImportService.cs` | `Enset.Application.Imports.DuplicationCheck.Services` | auskommentierter monolithischer Importentwurf | keine eigenständige plausible Nutzung neben Coordinator/Commit/Writer | D | nach referenzfreiem Buildnachweis entfernen; Verantwortung ist durch verbindliche getrennte Komponenten ersetzt |
| `DuplicationCheck/Resolutions/ConsoleImportIssueResolutionService.cs` | `Enset.Application.Imports.Resolution` | interaktive Console mutiert Issues direkt | konkurrierender manueller Resolutionpfad | D | entfernen/allenfalls in ein Sample außerhalb Application verschieben; API/Application-Use-Case ist verbindlich |
| `Reports/AutoFixReport.cs` | `Enset.Application.Imports.Reports` | ungenutzter Zählerbericht für generierte IDs | Bericht über vorgeschlagene oder bestätigte Korrekturen | B | nur mit expliziter Approval-Semantik behalten und umbenennen; aktuelle AutoFix-Terminologie ist irreführend |
| `Web/features/imports/hooks/useImportWizard.ts` | kein TS-Namespace; Featuremodul | leer | API-gebundene Wizard-Orchestrierung | A | für reale Frontendanbindung behalten; Reducer/Service nutzen |
| `Web/features/imports/components/models/WizardState.ts` | kein TS-Namespace | leer | typisierter Wizard-/Requestzustand | B | nach `features/imports/model` verschieben und als discriminated union implementieren |
| `Web/features/imports/components/models/WizardStep.ts` | kein TS-Namespace | leer | zweiter Step-Typ | D | entfernen; `features/imports/types/ImportWizardStep.ts` ist bereits die aktive eindeutige Definition |

Damit sind drei Platzhalter bzw. Altpfade Kategorie D. Alle anderen bleiben als plausible Phase-3- oder spätere Domainvorbereitung bestehen; ihre bloße Leere ist kein Löschgrund.

## 18. Konsistenz des Reviews und der Dokumentation

Das Review V1.2 ist als Ausgangsbasis fachlich belastbar: Die P0-Befunde Resolution-Payload, Repository-Semantik, Concurrency, EF-Roundtrip und Builddefekt sind durch den aktuellen Code belegt und bestimmen unmittelbar die Phase-3-Reihenfolge. Korrigiert wurde lediglich die zu pauschale frühere Behandlung leerer Dateien; Abschnitt 17 ersetzt diese durch die verlangte Einzelbewertung.

Die übrige Dokumentation ist nicht ausreichend aktuell, um allein als Phase-3-Spezifikation zu dienen:

- `docs/01_Architecture.md` nennt React, OpenAPI und DB-Reportpersistenz als nicht implementiert, obwohl entsprechende Artefakte vorhanden sind.
- `docs/04_Import.md` verwendet noch `ImportCommitRequest` und beschreibt Resolution als ausreichend, ohne die fehlende Payload-Anwendung hervorzuheben.
- `docs/06_API.md` behauptet OpenAPI/Swagger sei offen und nennt beim Multipart-Feld `file`, während der Vertrag `ImportFile` generiert.
- `docs/07_Frontend.md` behauptet, es existiere kein Frontend; tatsächlich baut ein React/Vite-Projekt, dessen Wizard aber noch Mockzustand nutzt.
- `docs/08_Data_Model.md` beschreibt nur JSON-Persistenz und erwähnt die neuen EF-Entities/Migration nicht.
- `docs/11_Roadmap.md` markiert OpenAPI und React-Aufsetzung fälschlich als offen und trennt den neuen relationalen Workflowtabellen-Slice noch nicht aus.
- `README.md` verweist auf die ältere V1.2 unter `docs/Decisions`, nicht auf die aktuelle Root-Fassung; dadurch existieren zwei Dokumente mit derselben Versionsbezeichnung und unterschiedlichem Geltungsstand.

Diese Dokumentationsbereinigung ist wichtig, blockiert aber nicht den Beginn des relationalen Foundation-Slices, sofern dieses Root-Review als verbindliche Phase-3-Entscheidung verwendet wird. Vor Merge der Phase-3-Implementierung müssen README und Detaildokumente synchronisiert und die doppelte V1.2-Ablage eindeutig aufgelöst werden.

## 19. Abschließendes Phase-3-Gate

| Gate | Aktueller Status | Entscheidung |
|---|---|---|
| Kernprojektgrenzen und Verantwortlichkeiten | ausreichend klar | GO |
| Analyse/Commit/WriteGate-Trennung | strukturell vorhanden | GO |
| Build- und Testbaseline | Worker und Tests kompilieren nicht | NO-GO bis Compilefix |
| relationales Workflowmodell | nur partiell vorhanden | GO für Foundation, NO-GO für produktiven Writer |
| vollständiger resolved Payload | fehlt | NO-GO für Writer |
| Repository-Contract und EF-Roundtrip | inkonsistent/unvollständig | NO-GO bis Contracttests grün |
| Concurrency/Idempotenz | fehlt | NO-GO für Commit-Aktivierung |
| Customer-Domainmodell | als Ausgangspunkt vorhanden | GO nach Identitätsentscheidung |
| PostgreSQL-Konfiguration/DI | nicht vorhanden; Design-Time-Credentials hart codiert | NO-GO bis externalisiert |
| reale PostgreSQL-Integrationstests | fehlen | NO-GO für Abschluss des Slices |
| Raw-Zone-Ziel | Port/Filesystem-Prototyp vorhanden | GO für Port, NO-GO für produktiven Object-Storage-Nachweis |

**Endentscheidung:** Das Repository und das ergänzte Review sind ausreichend vollständig und konsistent, um Phase 3 als kontrollierte Implementierungsphase zu eröffnen. Der erste Arbeitsschritt ist jedoch der relationale Workflow-/Concurrency-/Payload-Foundation-Slice, nicht der direkte Domainwrite. `DatabaseImportWriter` bleibt bis zum erfolgreichen Abschluss der Gates in Abschnitt 16.5 sicher blockierend. Danach kann der Customer-Vertical-Slice ohne großflächige Restrukturierung innerhalb der bestehenden Kernarchitektur umgesetzt werden.
# Historischer Review-Hinweis

Dieses Dokument bleibt als vorheriger Review-Stand erhalten. Für den Architecture Freeze gilt [ARCHITECTURE_REVIEW_V1_0_RC.md](ARCHITECTURE_REVIEW_V1_0_RC.md) zusammen mit der [Architecture Baseline 1.0 RC](../ARCHITECTURE_BASELINE_V1_0_RC.md).
