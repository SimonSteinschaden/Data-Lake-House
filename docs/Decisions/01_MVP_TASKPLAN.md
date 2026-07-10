# ENSET Data Lake House MVP - Detailaufgabenplan

Verbindliche Grundlage: ENSET Architecture Baseline v1.0, Repository-Stand mit `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`, `Enset.Api`, `Enset.Worker` und Tests, sowie die ENSET-Präsentation zum Data Lake House MVP und ENSET Universe.

Priorität:

- MUST: Bestandteil MVP Version 1.0
- SHOULD: Version 1.1
- COULD: spätere Erweiterung

Status:

- Abgeschlossen: implementiert und im Repository nachweisbar
- In Arbeit: strukturell vorbereitet, aber nicht produktionsreif vollständig
- Offen: noch nicht implementiert

## Bereits abgeschlossen

### Domain

- [x] Core Domain Entities
  - Beschreibung: `Customer`, `Project`, `Building`, `Meter`, `MeterReading`, `EnergySystem`, `Document`, `CalculationResult`, `BenchmarkDataset` und Geography-Modelle sind vorhanden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: keine
  - Betroffene Projekte: `Enset.Domain`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] Zeitreihenmodell für Messwerte
  - Beschreibung: `MeterReading` ist als Zeitreihenobjekt mit fachlicher Zählerzuordnung und Composite Key `MeterId + Timestamp` modelliert.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Core Domain Entities
  - Betroffene Projekte: `Enset.Domain`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] Trennung fachlicher und technischer Zähleridentität
  - Beschreibung: `Meter.MeterNumber` ist fachliche Identität; GUID bleibt technische Entity-Identität.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Meter-Modell
  - Betroffene Projekte: `Enset.Domain`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

### Application

- [x] Application-gesteuerter Analyseworkflow
  - Beschreibung: `ImportCoordinator` orchestriert `Read -> Map -> Validate -> DuplicateCheck -> ImportReport` ohne Schreibzugriffe.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Import Ports, Mapper, Validator, DuplicationCheck
  - Betroffene Projekte: `Enset.Application`, `Enset.Worker`, `Enset.Api`
  - Geschätzter Aufwand: erledigt

- [x] Strukturierte Importmodelle
  - Beschreibung: `ImportReport`, `ImportIssue`, `ImportDecision`, `ImportAuditEntry`, `ImportSourceFileMetadata` und Importstatus sind vorhanden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Importanalyse
  - Betroffene Projekte: `Enset.Application`, `Enset.Api`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] Resolution Workflow
  - Beschreibung: `ApplyResolutionService` erfasst Benutzerentscheidung, ResolutionAction, Custom Values und Audit Trail.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportIssue, persistierter ImportReport
  - Betroffene Projekte: `Enset.Application`, `Enset.Api`
  - Geschätzter Aufwand: erledigt

- [x] Write Gate
  - Beschreibung: `ImportWriteGate` blockiert Commits bei falschem Status, Abort, fehlendem User-Kontext oder offenen entscheidungspflichtigen Issues.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportReport, ImportStatus, Resolution Workflow
  - Betroffene Projekte: `Enset.Application`
  - Geschätzter Aufwand: erledigt

- [x] Commit Use Case
  - Beschreibung: `ImportCommitService` ruft Writer erst nach erfolgreichem Gate auf und protokolliert Commit-Audit.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Write Gate, Report Repository, Writer-Port
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`, `Enset.Api`
  - Geschätzter Aufwand: erledigt

### Infrastructure

- [x] Excel Reader
  - Beschreibung: Excel-Importadapter mit ClosedXML liegt in Infrastructure und bleibt außerhalb von Domain/Application.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Import Reader Port
  - Betroffene Projekte: `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] Excel Writer
  - Beschreibung: `ExcelImportWriter` kann freigegebene Imports in eine Zielarbeitsmappe schreiben.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportWriteContext, Write Gate, Infrastructure Workbook Writer
  - Betroffene Projekte: `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] JSON ImportReport Repository
  - Beschreibung: Dateibasierte Persistenz für Reports, Status, Source-Metadaten und Audit Trail ist vorhanden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportReport
  - Betroffene Projekte: `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] Raw Zone File Writer
  - Beschreibung: Originaldateien können nach ImportId dateibasiert archiviert werden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: SourceFileMetadata, Commit Use Case
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] EF Core DbContext und Migrationen
  - Beschreibung: PostgreSQL/Npgsql-Persistenzmodell für Domain Entities ist vorhanden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Domainmodell
  - Betroffene Projekte: `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

### REST API

- [x] ASP.NET Core API Projekt
  - Beschreibung: `Enset.Api` stellt einen HTTP Composition Root bereit.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Application Use Cases
  - Betroffene Projekte: `Enset.Api`
  - Geschätzter Aufwand: erledigt

- [x] Import-Endpunkte
  - Beschreibung: Analyze, Reportabruf, Resolutions und Commit sind unter `/api/v1/imports` vorhanden.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportAnalysisService, ReportRepository, ApplyResolutionService, CommitService
  - Betroffene Projekte: `Enset.Api`, `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

- [x] API bleibt Kommunikationsschicht
  - Beschreibung: Controller verwenden Application Services und greifen nicht direkt auf Writer oder Businesslogik zu.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Application Ports
  - Betroffene Projekte: `Enset.Api`
  - Geschätzter Aufwand: erledigt

### React Frontend

- [x] Keine erledigten Frontend-Aufgaben
  - Beschreibung: Im Repository existiert noch kein React-Projekt.
  - Status: Abgeschlossen als Befund
  - Priorität: MUST
  - Abhängigkeiten: REST API
  - Betroffene Projekte: keine
  - Geschätzter Aufwand: erledigt als Analyse

### Data Products

- [x] Analyse-Entities vorbereitet
  - Beschreibung: `CalculationResult` und `BenchmarkDataset` existieren als persistierbare Ergebnisstrukturen.
  - Status: Abgeschlossen
  - Priorität: SHOULD
  - Abhängigkeiten: Domainmodell, DbContext
  - Betroffene Projekte: `Enset.Domain`, `Enset.Infrastructure`
  - Geschätzter Aufwand: erledigt

### Security

- [x] Audit-User-Kontext im Importpfad
  - Beschreibung: Analyze und Resolution/Commit erfassen derzeit einen User-Kontext über Header beziehungsweise Request.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: ImportReport, Audit Trail
  - Betroffene Projekte: `Enset.Api`, `Enset.Application`
  - Geschätzter Aufwand: erledigt

### Deployment

- [x] Lokale API- und Worker-Composition Roots
  - Beschreibung: API und Worker können Services zusammenstellen; produktive Konfiguration ist noch offen.
  - Status: Abgeschlossen
  - Priorität: SHOULD
  - Abhängigkeiten: Application/Infrastructure Services
  - Betroffene Projekte: `Enset.Api`, `Enset.Worker`
  - Geschätzter Aufwand: erledigt

### Testing

- [x] Architektur- und Workflowtests
  - Beschreibung: Tests sichern Analyze-only Coordinator, Report Repository, Resolution, Write Gate, Commit und API-Grenze.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Importworkflow
  - Betroffene Projekte: `Enset.Import.Tests`
  - Geschätzter Aufwand: erledigt

### Documentation

- [x] Architektur-, Import-, API- und Roadmap-Dokumentation
  - Beschreibung: `docs/` beschreibt aktuellen Stand, Zielgrenzen, offene Punkte und Roadmap bis 1.0.
  - Status: Abgeschlossen
  - Priorität: MUST
  - Abhängigkeiten: Repository-Analyse
  - Betroffene Projekte: `docs`
  - Geschätzter Aufwand: erledigt

## In Arbeit

### Domain

- [ ] DataProduct als Domain-/Contract-Konzept abgrenzen
  - Beschreibung: Präsentation und Architektur zeigen Data Products als konsumierbare Gold-Zone; im Code fehlen noch Verträge und Metadaten.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: Curated Data, CalculationResult, BenchmarkDataset
  - Betroffene Projekte: `Enset.Domain`, `Enset.Application`
  - Geschätzter Aufwand: 1-2 PT

### Application

- [ ] Building-, Meter- und MeterReading-Import vollständig durchziehen
  - Beschreibung: Customer-Importpfad ist vorbereitet; Building, Meter und MeterReading müssen Ende-zu-Ende validiert, gemappt und reportet werden.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: DTOs, Excel Reader, Validator, Domainmodell
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`, `Enset.Import.Tests`
  - Geschätzter Aufwand: 4-6 PT

- [ ] ImportHistory und Wiederaufnahme fachlich modellieren
  - Beschreibung: Importstatus existiert, aber dauerhafte History, Resume und Concurrency Control sind nicht produktiv umgesetzt.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: ImportReport, Repository, API
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`, `Enset.Api`
  - Geschätzter Aufwand: 3-5 PT

### Infrastructure

- [ ] DatabaseImportWriter produktiv implementieren
  - Beschreibung: Aktuell ist der Writer ein sicherer Platzhalter; MVP benötigt transaktionales Schreiben validierter Daten in PostgreSQL/TimescaleDB.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: vollständiges Mapping, Write Gate, ImportHistory, DbContext
  - Betroffene Projekte: `Enset.Infrastructure`, `Enset.Application`
  - Geschätzter Aufwand: 5-8 PT

- [ ] PostgreSQL/TimescaleDB-Strategie finalisieren
  - Beschreibung: Modell ist Timescale-kompatibel; Hypertable-Betrieb, Migration und Deployment-Nachweis fehlen.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: DatabaseImportWriter, Docker Compose
  - Betroffene Projekte: `Enset.Infrastructure`, `docs`
  - Geschätzter Aufwand: 2-4 PT

### REST API

- [ ] OpenAPI-Vertrag produktiv machen
  - Beschreibung: Swagger ist technisch eingebunden; stabiler Vertrag, Fehlerbeispiele und Client-Generation fehlen.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: stabile Request-/Response-DTOs
  - Betroffene Projekte: `Enset.Api`, `docs`
  - Geschätzter Aufwand: 1-2 PT

### Data Products

- [ ] Minimaler Data Product Port
  - Beschreibung: `IDataProductPublisher` und `IDataProductQueryPort` müssen als Grenze eingeführt werden, damit Business Modules niemals direkt Storage konsumieren.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: DataProduct Contract, Curated Write
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 2-4 PT

### Security

- [ ] Authentifizierung und Autorisierung integrieren
  - Beschreibung: UserId darf nicht dauerhaft aus frei übermitteltem Header/Body kommen; Zielbild nennt OAuth2/OpenID Connect und rollenbasierten Zugriff.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: API, Rollenmodell, Konfiguration
  - Betroffene Projekte: `Enset.Api`, `Enset.Application`, `docs`
  - Geschätzter Aufwand: 3-5 PT

### Deployment

- [ ] Externe Konfiguration
  - Beschreibung: Pfade und Connection Strings müssen aus `appsettings`, Environment Variables oder Secrets kommen.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: API/Worker Composition Roots
  - Betroffene Projekte: `Enset.Api`, `Enset.Worker`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 1-2 PT

### Testing

- [ ] Import-End-to-End-Tests verbreitern
  - Beschreibung: Tests müssen Excel Analyze, Resolution, DB Commit, Raw Archivierung und API-Fehlerpfade abdecken.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: DatabaseImportWriter, API, Test Fixtures
  - Betroffene Projekte: `Enset.Import.Tests`, `Enset.Api`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 4-6 PT

### Documentation

- [ ] MVP-Plan und Timeline festschreiben
  - Beschreibung: Die vier Decision-Dokumente werden als offizieller Projektplan erstellt.
  - Status: In Arbeit
  - Priorität: MUST
  - Abhängigkeiten: Repository- und Präsentationsanalyse
  - Betroffene Projekte: `docs/Decisions`
  - Geschätzter Aufwand: 1 PT

## Offen

### Domain

- [ ] ImportJob und DataSource persistenzfähig schärfen
  - Beschreibung: Prozess- und Herkunftsdaten müssen referenziell mit Raw, Curated und ImportReport verbunden werden.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: ImportHistory, DbContext-Migration
  - Betroffene Projekte: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 2-3 PT

- [ ] DataProduct Contract Version 1
  - Beschreibung: Standardisierte Produktmetadaten für Scope, Version, Qualität, Provenance, Gültigkeit und Veröffentlichung definieren.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: DataProduct Port, Curated Data
  - Betroffene Projekte: `Enset.Domain`, `Enset.Application`
  - Geschätzter Aufwand: 2-3 PT

### Application

- [ ] Curated Write Pipeline
  - Beschreibung: Nach Freigabe müssen validierte Daten kontrolliert in Curated/Silver überführt werden.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: DatabaseImportWriter, ImportJob, DataSource
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 4-6 PT

- [ ] Minimaler Data Product Publisher
  - Beschreibung: Nach Curated Write werden MVP-Data-Products erzeugt oder mindestens als versionierter Produktstatus publiziert.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: DataProduct Contract, Calculation/Benchmark-Grundlage
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 3-5 PT

- [ ] Projekt-Use-Cases
  - Beschreibung: Präsentation nennt Projekt Use Cases; MVP benötigt mindestens lesende/verwaltende Use Cases für importierte Projekte und Zuordnungen.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: Domainpersistenz, API Auth
  - Betroffene Projekte: `Enset.Application`, `Enset.Api`
  - Geschätzter Aufwand: 3-5 PT

### Infrastructure

- [ ] Datenbankgestützte Report- und ImportHistory-Persistenz
  - Beschreibung: JSON-Repository ist MVP-naher Übergang; produktiver Betrieb benötigt DB-Persistenz mit Concurrency Control.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: ImportReport Entity Mapping, Migrationen
  - Betroffene Projekte: `Enset.Infrastructure`
  - Geschätzter Aufwand: 4-6 PT

- [ ] Produktive Raw Zone
  - Beschreibung: Dateisystem-Archivierung muss um Retention, Zugriffsschutz, Prüfsumme und Betriebspfad ergänzt werden.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: externe Konfiguration, Deployment-Ziel
  - Betroffene Projekte: `Enset.Infrastructure`, `Enset.Api`
  - Geschätzter Aufwand: 2-4 PT

- [ ] Logging Adapter produktiv machen
  - Beschreibung: Konsolenlogging durch strukturiertes `ILogger`/Serilog-kompatibles Logging mit ImportId/CorrelationId ersetzen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: API/Worker Host
  - Betroffene Projekte: `Enset.Api`, `Enset.Worker`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 1-3 PT

### REST API

- [ ] Auth-gesicherte Import API
  - Beschreibung: Endpunkte mit JWT Bearer/OIDC absichern und Rollen für Analyse, Resolution und Commit trennen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: Identity Provider, Rollenmodell
  - Betroffene Projekte: `Enset.Api`
  - Geschätzter Aufwand: 3-5 PT

- [ ] API-Fehlerverträge und Limits
  - Beschreibung: Einheitliche ProblemDetails, Upload-Limits, Content-Type-Prüfung und fachliche Fehlercodes ergänzen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: OpenAPI-Vertrag
  - Betroffene Projekte: `Enset.Api`
  - Geschätzter Aufwand: 2-3 PT

- [ ] ImportHistory API
  - Beschreibung: Liste, Filter, Status und Wiederaufnahme von Importen bereitstellen.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: DB-gestützte ImportHistory
  - Betroffene Projekte: `Enset.Api`, `Enset.Application`
  - Geschätzter Aufwand: 2-4 PT

### React Frontend

- [ ] React Import Wizard
  - Beschreibung: Upload, Analyze, Issue-Gruppierung, Resolution, Commit und Ergebnisstatus als Web UI umsetzen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: REST API, OpenAPI-Vertrag, Auth
  - Betroffene Projekte: neues React-Projekt
  - Geschätzter Aufwand: 6-10 PT

- [ ] ImportHistory UI
  - Beschreibung: Importstatus, offene Entscheidungen, abgeschlossene Imports und Fehler anzeigen.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: ImportHistory API
  - Betroffene Projekte: neues React-Projekt, `Enset.Api`
  - Geschätzter Aufwand: 3-5 PT

- [ ] Dashboard-Grundlage
  - Beschreibung: Erste MVP-Ansicht für importierte Daten und DataProduct-Status vorbereiten.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: DataProduct Query Port
  - Betroffene Projekte: neues React-Projekt, `Enset.Api`
  - Geschätzter Aufwand: 3-6 PT

### Data Products

- [ ] MVP Data Product: Objektprofil
  - Beschreibung: Aus Präsentation ableitbares Data Product für Objekt/Gebäude mit Verbrauchsprofilen, Anlagenperformance und Kennzahlen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: Curated Building/Meter/MeterReading Daten
  - Betroffene Projekte: `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`, `Enset.Api`
  - Geschätzter Aufwand: 4-6 PT

- [ ] MVP Data Product: Gemeinde-/Quartiersaggregat
  - Beschreibung: Aggregierte Verbrauchs- und Benchmark-Grundlage ohne Rückschluss auf einzelne Kunden.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: Anonymisierung, Mindestgruppengröße, CalculationResult
  - Betroffene Projekte: `Enset.Application`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 4-7 PT

- [ ] Data Product Delivery
  - Beschreibung: Bereitstellung über Dashboard, API oder Export vorbereiten; keine Business-Module-Direktzugriffe auf Storage.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: DataProduct Query Port, API Auth
  - Betroffene Projekte: `Enset.Application`, `Enset.Api`, React Frontend
  - Geschätzter Aufwand: 3-5 PT

### Security

- [ ] Rollen- und Rechtekonzept
  - Beschreibung: Rollen für Administrator, Energieberater und Sachbearbeiter aus Präsentation ableiten und technisch abbilden.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: Auth Integration
  - Betroffene Projekte: `Enset.Api`, React Frontend, `docs`
  - Geschätzter Aufwand: 2-3 PT

- [ ] Audit und Datenschutz härten
  - Beschreibung: Audit Trail unveränderbarer machen und interne Pfade/Personenbezug in Responses begrenzen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: ImportHistory DB, API Fehlerverträge
  - Betroffene Projekte: `Enset.Application`, `Enset.Api`, `Enset.Infrastructure`
  - Geschätzter Aufwand: 2-4 PT

### Deployment

- [ ] Docker und Docker Compose
  - Beschreibung: Container für API, PostgreSQL/TimescaleDB und optional Identity Provider bereitstellen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: externe Konfiguration, DB-Migrationen
  - Betroffene Projekte: Repository Root, `src`
  - Geschätzter Aufwand: 3-5 PT

- [ ] CI Pipeline
  - Beschreibung: Restore, Build, Test, Architekturtests und Migrationsprüfung automatisieren.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: stabile Projektstruktur
  - Betroffene Projekte: Repository Root, `tests`
  - Geschätzter Aufwand: 2-3 PT

- [ ] Betriebsmonitoring
  - Beschreibung: Health Checks, strukturierte Logs und Basis-Metriken vorbereiten.
  - Status: Offen
  - Priorität: SHOULD
  - Abhängigkeiten: Docker, Logging
  - Betroffene Projekte: `Enset.Api`, Deployment-Konfiguration
  - Geschätzter Aufwand: 2-4 PT

### Testing

- [ ] API-Integrationstests
  - Beschreibung: Analyze, Get, Resolutions und Commit inklusive Fehlerfällen über HTTP testen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: API Fehlerverträge, Testhost
  - Betroffene Projekte: `Enset.Import.Tests`, `Enset.Api`
  - Geschätzter Aufwand: 3-5 PT

- [ ] Datenbank-Integrationstests
  - Beschreibung: Migration, DB Commit, Timescale/Hypertable und Transaktionsverhalten testen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: Docker Compose, DatabaseImportWriter
  - Betroffene Projekte: `Enset.Infrastructure`, `Enset.Import.Tests`
  - Geschätzter Aufwand: 4-6 PT

- [ ] Frontend E2E-Tests
  - Beschreibung: Import Wizard Workflow über Upload, Resolution und Commit testen.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: React Import Wizard, API Testumgebung
  - Betroffene Projekte: React Frontend, `Enset.Api`
  - Geschätzter Aufwand: 2-4 PT

### Documentation

- [ ] API- und Betriebsdokumentation
  - Beschreibung: OpenAPI, Deployment, Konfiguration, Importbetrieb und Troubleshooting dokumentieren.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: API Vertrag, Docker, Auth
  - Betroffene Projekte: `docs`
  - Geschätzter Aufwand: 2-3 PT

- [ ] Abschlussreview MVP 1.0
  - Beschreibung: Gegen Architecture Baseline v1.0 prüfen, insbesondere DataProduct-Grenze, REST-Kommunikationsschicht und DLH als Source of Truth.
  - Status: Offen
  - Priorität: MUST
  - Abhängigkeiten: alle MUST-Aufgaben
  - Betroffene Projekte: gesamtes Repository
  - Geschätzter Aufwand: 1-2 PT
