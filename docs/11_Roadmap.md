# Roadmap bis Version 1.0

Die Roadmap konkretisiert ausschließlich die noch offenen Arbeiten der verbindlichen Architecture Baseline v1.0. Sie definiert keine neue Zielarchitektur.

## Meilenstein 1: Importworkflow absichern

- [x] Application-gesteuerten `ImportCoordinator` einführen
- [x] Analyse von Schreibzugriffen trennen
- [x] `ImportIssue`, `ImportReport` und `ImportDecision` strukturieren
- [x] `ApplyResolutionService`, `ImportWriteContext` und `ImportWriteGate` bereitstellen
- [x] ApplyResolutionService mit Reportpersistenz und gemeinsamen API-/Console-Aufrufern integrieren
- [x] Benutzer-/Zeit-/Audit-Metadaten für Entscheidungen ergänzen
- [x] Kernprüfungen für Coordinator, Repository, ResolutionService, WriteGate, Commit und API-Grenze implementieren
- [ ] vollständige Building-, Meter- und MeterReading-Verarbeitung ergänzen

## Meilenstein 2: Persistenz- und Storage-Pfade

- [x] `ImportReport` und Importstatus dateibasiert persistent speichern
- [ ] `ImportHistory` und Wiederaufnahme eines Imports implementieren
- [ ] `DatabaseImportWriter` transaktional implementieren
- [x] dateibasierten `RawZoneWriter` für unveränderte Quelldateien implementieren
- [ ] dateibasierte Report-/Raw-Persistenz durch produktive Storage-Backends ersetzen
- [ ] Curated-/Silver-Schreibpfad und Provenance absichern
- [ ] `ImportJob` und `DataSource` vollständig in die Persistenz integrieren
- [ ] TimescaleDB-Hypertable- und Migrationsstrategie verifizieren

## Meilenstein 3: REST API

- [x] ASP.NET-Core-API-Projekt erstellen
- [x] Importanalyse, Reportabruf, Resolution und Commit als Endpunkte bereitstellen
- [x] grundlegendes Request-/Response-Mapping und Validierung implementieren
- [ ] OpenAPI-/Swagger-Dokumentation erstellen
- [ ] Authentifizierung und Autorisierung integrieren
- [ ] API-Integrations- und Sicherheitstests ergänzen

## Meilenstein 4: React Import Wizard

- [ ] React-Frontend aufsetzen
- [ ] Upload und Analysefortschritt darstellen
- [ ] Issues gruppieren und ResolutionActions erfassen
- [ ] Bestätigung und Ergebnisstatus abbilden
- [ ] Accessibility-, UI- und End-to-End-Tests ergänzen

## Meilenstein 5: Betrieb und Automatisierung

- [ ] Worker auf Generic Host und externe Konfiguration umstellen
- [ ] Background Jobs, Queueing, Retry und Idempotenz implementieren
- [ ] strukturiertes Logging, Monitoring und Fehlerbehandlung ergänzen
- [ ] Secrets und Connection Strings externalisieren
- [ ] Docker/Compose, CI/CD und reproduzierbare Entwicklungsumgebung ergänzen
- [ ] versionierte Build-Artefakte bereinigen und `.gitignore` einführen

## Meilenstein 6: Baseline vollständig nachweisen

- [ ] Data-Product-Ports und Publikationspfad implementieren
- [ ] Raw-, Curated- und Data-Product-Grenzen technisch durchsetzen
- [ ] KPI-/Benchmark-Verarbeitung vervollständigen
- [ ] vorhandene Kern- und Architekturtests um Integrations-, Sicherheits- und End-to-End-Tests erweitern
- [ ] API- und Betriebsdokumentation fertigstellen
- [ ] Abschlussreview gegen `ARCHITECTURE_REVIEW_V1_0.md` durchführen
