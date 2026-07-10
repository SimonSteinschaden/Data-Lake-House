# ENSET Universe - Langfristiger Projektzeitplan

Dieser Zeitplan enthält bewusst keine festen Datumsangaben. Er beschreibt die logische Reihenfolge, technische Abhängigkeiten und mögliche Parallelisierung für das ENSET Universe gemäß Repository, Präsentation und Architecture Baseline v1.0.

## Phase 1 - Data Lake House Foundation

### Meilenstein

MVP-fähige DLH-Basis mit Import, Raw Zone, Curated Zone, DataProduct-Grundlage und REST API.

### Ergebnis

- Backend mit Domain, Application, Infrastructure und API.
- React Import Wizard.
- Authentifizierter Importworkflow.
- PostgreSQL/TimescaleDB-Persistenz.
- Erste Data Products.
- Docker/CI und Basisdokumentation.

### Abhängigkeiten

- Bestehende Importarchitektur.
- DatabaseImportWriter.
- Curated Write Pipeline.
- DataProduct Contract.
- Authentifizierung und Deployment.

### Mögliche Parallelisierung

- Backend-DB-Persistenz und React UI können parallel laufen, sobald API DTOs stabil sind.
- Docker/CI kann parallel zur Implementierung des DB Writers starten.
- OpenAPI-Dokumentation kann parallel zur Frontend-Anbindung wachsen.

## Phase 2 - Platform Hardening

### Meilenstein

Produktionsnahe Betriebsfähigkeit.

### Ergebnis

- Externe Konfiguration und Secrets.
- Containerisierung.
- Deployment-Prozess.
- Health Checks.
- Monitoring und Logging.
- Backup/Restore.
- ImportHistory und Background Jobs.

### Abhängigkeiten

- MVP-API.
- Datenbankmigrationen.
- Auth-Konzept.
- Betriebszielumgebung.

### Mögliche Parallelisierung

- Logging/Monitoring kann parallel zu Backup/Restore aufgebaut werden.
- CI/CD kann parallel zu Docker- und Migrationsarbeit erweitert werden.

## Phase 3 - Administration und Benutzerverwaltung

### Meilenstein

Plattformrollen, Organisationen und Rechte sind nutzbar.

### Ergebnis

- Benutzerverwaltung über Identity Provider.
- Rollen und Rechte für Administratoren, Energieberater, Sachbearbeiter und spätere Organisationsrollen.
- Audit und Protokollierung.
- Mandanten-/Organisationsverwaltung, soweit für die Plattform nötig.

### Abhängigkeiten

- Authentifizierung.
- API Security.
- Audit Trail.
- Frontend Shell.

### Mögliche Parallelisierung

- UI-Administration kann parallel zu Rollen-/Policy-Implementierung laufen.
- Audit-Auswertung kann parallel zu ImportHistory erweitert werden.

## Phase 4 - Data Product Platform

### Meilenstein

Data Products sind die verbindliche Schnittstelle für Business Modules.

### Ergebnis

- DataProduct Publisher.
- DataProduct Query Port.
- Versionierung, Qualität, Provenance und Katalog.
- Bereitstellung über Dashboards, APIs, Exporte, Events oder Webhooks.
- Governance-Regeln für Freigabe und Zugriff.

### Abhängigkeiten

- Curated Data.
- Calculation/Benchmark Services.
- Security/Rollen.
- API Layer.

### Mögliche Parallelisierung

- DataProduct-Katalog und erste Produktgeneratoren können parallel entstehen.
- API-Auslieferung und Dashboard-Anzeige können parallel implementiert werden.

## Phase 5 - Reporting und Benchmarking

### Meilenstein

Nutzer erhalten verlässliche Reports und Vergleichswerte.

### Ergebnis

- Reporting Service.
- Benchmarking Data Products.
- Dashboards für Objekt, Quartier, Gemeinde und Region.
- Datenschutzkonforme Aggregation und Mindestgruppengrößen.
- Export- und Berichtsfunktionen.

### Abhängigkeiten

- DataProduct Platform.
- KPI- und Benchmarkmodelle.
- Frontend Dashboard-Grundlage.
- Berechtigungskonzept.

### Mögliche Parallelisierung

- Backend-KPI-Services und Frontend-Dashboard-Komponenten können parallel laufen.
- Reportexport kann parallel zu Benchmark-Aggregation entstehen.

## Phase 6 - Integration Layer und externe Datenquellen

### Meilenstein

Externe Systeme und Datenquellen sind standardisiert angebunden.

### Ergebnis

- Adapter für Dateien, APIs, Smart Meter, IoT, EMS/Gebäudeautomation, Wetterdaten, Netzbetreiber und Marktdaten.
- Messaging/Event-Grundlage.
- Datenqualitätsprüfungen je Quelle.
- Katalogisierte DataSource-Provenance.

### Abhängigkeiten

- Import-/Ingestion-Pattern aus dem MVP.
- Raw Zone und Curated Zone.
- Auth und Secrets.
- Monitoring.

### Mögliche Parallelisierung

- Einzelne Adapter können unabhängig entwickelt werden.
- Datenqualitätsregeln können parallel zur technischen Konnektivität entstehen.

## Phase 7 - Energy Data Platform

### Meilenstein

Operative Plattformoberfläche für Projekte, Dokumente, Workflows, Aufgaben und Benachrichtigungen.

### Ergebnis

- Projekt- und Dokumentverwaltung.
- Dashboards.
- Aufgaben und Workflows.
- Benachrichtigungen.
- Anwendungsorchestrierung über REST/API Layer.

### Abhängigkeiten

- Benutzerverwaltung.
- DataProduct Query Port.
- Projekt-Domainmodell.
- Frontend Shell.

### Mögliche Parallelisierung

- Projektverwaltung und Dashboard-Ansichten können parallel entstehen.
- Benachrichtigungen können parallel zu Workflow-Aufgaben vorbereitet werden.

## Phase 8 - Monitoring & Diagnose

### Meilenstein

Früherkennung von Anomalien und Diagnoseempfehlungen.

### Ergebnis

- Monitoring Data Products.
- Anomalieerkennung.
- Diagnose- und Ursachenhinweise.
- Empfehlungen und Handlungsoptionen.
- Betriebs- und Anlagenüberwachung.

### Abhängigkeiten

- Integration Layer.
- Zeitreihen- und Betriebsdaten.
- DataProduct Platform.
- Logging/Monitoring-Infrastruktur.

### Mögliche Parallelisierung

- Anomalieerkennung und UI-Diagnoseansicht können parallel entstehen.
- Alerting kann parallel zur Empfehlungslogik entwickelt werden.

## Phase 9 - Forecasting

### Meilenstein

Prognosedaten für Planung, Monitoring und Optimierung.

### Ergebnis

- Verbrauchsprognosen.
- Erzeugungsprognosen.
- Preis- und Marktsignale.
- Lastspitzenprognosen.
- Szenario- und Planungsgrundlagen.

### Abhängigkeiten

- Historische DLH-Daten.
- Wetter-, Kalender-, Markt- und Netzdaten.
- Modellversionierung.
- DataProduct Publisher.

### Mögliche Parallelisierung

- Wetter-/Preisadapter und Forecast-Services können parallel laufen.
- Szenario-UI kann parallel zu ersten Prognoseprodukten entstehen.

## Phase 10 - Load Management

### Meilenstein

Optimierungsvorschläge zur Vermeidung von Lastspitzen.

### Ergebnis

- Lastmanagement Data Products.
- Prioritäten und Parameter.
- Optimierungsvorschläge.
- Manuelle Freigabe und optionale automatische Aktionen.
- Ergebniskennzahlen zu Kosten, CO2 und Netzstabilität.

### Abhängigkeiten

- Forecasting.
- Anlagen-, Speicher-, Tarif- und Netzdaten.
- Benutzerkontrolle.
- Integration Layer für steuerbare Systeme.

### Mögliche Parallelisierung

- Optimierungsbackend und Kontroll-UI können parallel entstehen.
- Simulationsfunktionen können parallel zu manuellen Handlungsvorschlägen ausgebaut werden.

## Phase 11 - P2P Energy Trading

### Meilenstein

P2P-/EEG- und Negativstrompreis-Management.

### Ergebnis

- Markt- und P2P Data Products.
- Chancen- und Risikoerkennung.
- Strategievorschläge für Speicher, Verbrauch, Handel, Lieferung und Abregelung.
- Monitoring, Lernen und Performance Tracking.

### Abhängigkeiten

- Forecasting.
- Marktpreis- und Regulatorikdaten.
- Energy Communities Daten.
- Benutzerfreigabe, Audit und Governance.

### Mögliche Parallelisierung

- Marktadapter und Strategiemodell können parallel entstehen.
- Performance Tracking kann parallel zur ersten Entscheidungslogik aufgebaut werden.

## Phase 12 - KI & Optimierung

### Meilenstein

Kontinuierliche Verbesserung und intelligente Optimierung über Business Modules hinweg.

### Ergebnis

- Modelltraining auf freigegebenen Data Products.
- Empfehlungen.
- Szenariovergleich.
- Optimierungsparameter.
- Rückkopplung aus Monitoring und Benutzerentscheidungen.

### Abhängigkeiten

- DataProduct Platform.
- Forecasting.
- Monitoring.
- Auditierte Benutzerentscheidungen.
- Modellversionierung und Qualitätsmetriken.

### Mögliche Parallelisierung

- Modellmetadaten und erste Optimierungsmodelle können parallel entwickelt werden.
- UI für Parameter/Freigaben kann parallel zu Backend-Empfehlungen entstehen.

## Phase 13 - Data Space und Data Marketplace

### Meilenstein

Freigegebener organisationsübergreifender Datenaustausch.

### Ergebnis

- Data Space Connector.
- Governance und Policies.
- Katalog und Metadaten.
- Bereitstellung freigegebener Data Products.
- Optionaler Data Marketplace, soweit aus DataProduct-Katalog und Data-Space-Bereitstellung abgeleitet.

### Abhängigkeiten

- DataProduct Governance.
- Security und Vertrauensschicht.
- Zugriffskontrolle.
- Katalogisierte Produkte.

### Mögliche Parallelisierung

- Katalog/Marketplace-UI kann parallel zum Connector vorbereitet werden.
- Policies und Freigabeprozesse können parallel zu technischen Austauschformaten entstehen.

## Phase 14 - Business Modules Suite

### Meilenstein

ENSET Business Modules nutzen einheitlich Data Products.

### Ergebnis

- Municipal Building Platform.
- Energy Management.
- Energy Communities.
- Funding Management.
- Benchmarking.
- Reporting.
- ESG Management.
- CO2 Management.
- Weitere Module nur auf Basis freigegebener Data Products.

### Abhängigkeiten

- Energy Data Platform.
- Data Products.
- Auth/Rollen.
- Dashboards.
- API Layer.

### Mögliche Parallelisierung

- Module können parallel entwickelt werden, wenn ihre benötigten Data Products stabil sind.
- Gemeinsame Dashboard-, Reporting- und Admin-Komponenten sollten zentral wiederverwendet werden.

## Phase 15 - ENSET Universe Betrieb

### Meilenstein

Skalierbare Gesamtplattform mit stabiler Governance.

### Ergebnis

- Betriebsmodell für Core Platform, DLH, Integration Layer, IAM, API Layer, Data Space und Business Modules.
- Monitoring, Logging, Kostenkontrolle und Performance-Management.
- Release- und Migrationsprozesse.
- Governance für Datenqualität, Datenschutz, Data Products und externe Schnittstellen.

### Abhängigkeiten

- Alle Kernplattformphasen.
- Reife Deployment- und Observability-Prozesse.
- Administrations- und Sicherheitsmodell.

### Mögliche Parallelisierung

- Governance, Betrieb und Produktweiterentwicklung laufen dauerhaft parallel.
- Neue Business Modules werden nur ergänzt, wenn ihre DataProduct-Abhängigkeiten vorhanden und freigegeben sind.
