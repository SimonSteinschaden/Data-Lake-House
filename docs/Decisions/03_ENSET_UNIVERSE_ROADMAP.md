# ENSET Universe - Grobaufgabenplan

Diese Roadmap leitet sich ausschließlich aus Repository, ENSET-Präsentation und ENSET Architecture Baseline v1.0 ab. Verbindliche Architekturregeln:

- Das Data Lake House bleibt Source of Truth für historische Energie-, Analyse- und DataProduct-Daten.
- REST API ist Kommunikationsschicht, nicht Ort der Businesslogik.
- Business Modules konsumieren ausschließlich Data Products.
- Data Space dient Austausch freigegebener Data Products, nicht interner Verarbeitung.
- Integration Layer bindet externe Systeme und Datenquellen an.

## Phase 1 - Data Lake House MVP

### Ziel

Produktionsnaher MVP für Datenimport, Validierung, Benutzerentscheidung, Persistenz, Curated Data und erstes Data Product.

### Beschreibung

Der vorhandene Importanalysepfad wird zur vollständigen Data-Lake-House-Pipeline erweitert: Raw Zone, DTO Mapping, Validation, Duplicate Detection, Resolution, Commit, Curated Write und MVP DataProduct. React Frontend und REST API bilden den Bedien- und Kommunikationspfad.

### Abhängigkeiten

- Current Clean Architecture
- ImportCoordinator, ImportReport, WriteGate, REST API
- PostgreSQL/TimescaleDB
- Authentication
- Docker/CI

### Ergebnis

MVP Version 1.0 mit importierten, validierten und versioniert bereitgestellten Data Products.

## Phase 2 - Energy Data Platform

### Ziel

Anwendungsplattform für Dashboards, Projekte, Dokumente, Workflows, Aufgaben und Benachrichtigungen.

### Beschreibung

Die Energy Data Platform nutzt das Data Lake House über Application/API-Grenzen. Sie orchestriert Benutzerprozesse, Projektverwaltung und operative Workflows, erzeugt aber keine Data Products außerhalb der DLH-Pipeline.

### Abhängigkeiten

- MVP Import und DataProduct Query Port
- Authentifizierung und Rollen
- React Frontend
- Projekt- und Dokument-Use-Cases

### Ergebnis

Benutzer können Daten, Projekte, Aufgaben und erste Auswertungen in einer Plattformoberfläche bearbeiten.

## Phase 3 - Data Products

### Ziel

Standardisierte, vertrauenswürdige und wiederverwendbare Data Products für Objekt, Quartier, Gemeinde, Region und Bereitstellung.

### Beschreibung

Data Products werden aus Curated Data erzeugt, versioniert, mit Qualität/Provenance versehen und über Query Ports, APIs, Dashboards, Exporte oder Events bereitgestellt. Aus der Präsentation ableitbare Produktgruppen sind Objekt, Quartier, Gemeinde, Region und Bereitstellung.

### Abhängigkeiten

- Curated Zone
- CalculationResult und BenchmarkDataset
- DataProduct Publisher/Query Port
- Datenschutz- und Qualitätsregeln

### Ergebnis

Business Modules erhalten ausschließlich freigegebene Data Products.

## Phase 4 - Benchmarking

### Ziel

Vergleichswerte und Reports für Gebäude, Quartiere, Gemeinden und Regionen bereitstellen.

### Beschreibung

Benchmarking nutzt aggregierte, anonymisierte Data Products. Fachliche Kennzahlen basieren auf `CalculationResult`, `BenchmarkDataset`, Verbrauchsprofilen und regionalen Kontextdaten.

### Abhängigkeiten

- Data Products
- Mindestgruppengrößen und Anonymisierung
- Reporting Service
- KPI- und Benchmark-Services

### Ergebnis

Benchmark-Dashboards, Berichte und API-Abfragen ohne direkten Zugriff auf Roh- oder Curated-Storage.

## Phase 5 - Monitoring

### Ziel

Monitoring & Diagnose zur Früherkennung von Anomalien und Betriebsabweichungen.

### Beschreibung

Die Präsentation beschreibt Monitoring & Diagnose mit Sensoren/IoT, Betriebs- und Prozessdaten, Störhistorie, Wetter, Kontext, KI-Analyse, Empfehlungen und Maßnahmen. Diese Phase baut auf Data Products und optionalen Live-/Event-Daten aus dem Integration Layer auf.

### Abhängigkeiten

- DataProduct Query Port
- Integration Layer für Sensoren, EMS, SCADA/PLC oder IoT
- Monitoring/Logging-Infrastruktur
- Ereignis- und Benachrichtigungsmechanismen

### Ergebnis

Anomalieerkennung, Diagnose-Ergebnisse, Empfehlungen und Monitoring-Dashboards.

## Phase 6 - Forecasting

### Ziel

Prognosen für Verbrauch, Erzeugung, Preise, Lastspitzen und Szenarien.

### Beschreibung

Forecasting kombiniert historische DLH-Daten, Wetterprognosen, Marktpreise, Kalenderereignisse und Anlageninformationen. Die Ergebnisse werden als Data Products bereitgestellt und von Planungs-, Monitoring- und Optimierungsmodulen konsumiert.

### Abhängigkeiten

- Historische Zeitreihen im DLH
- Externe Wetter-, Preis- und Kalenderdaten
- DataProduct Pipeline
- Modellversionierung und Qualitätsmetriken

### Ergebnis

Versionierte Forecast Data Products für weitere Business Modules.

## Phase 7 - Load Management

### Ziel

Lastspitzen vermeiden, Kosten senken und Versorgungssicherheit erhöhen.

### Beschreibung

Die Präsentation zeigt Load Management mit Smart-Meter- und Live-Verbrauchsdaten, Erzeugungsanlagen, Speichern, Wetter, Tarifen, Netzdaten, KI-Prognose, Optimierungsvorschlägen und manueller/automatischer Aktion. Das Modul entscheidet auf Basis von Data Products und gibt Handlungsempfehlungen oder Aktionen aus.

### Abhängigkeiten

- Forecast Data Products
- Anlagen-, Speicher- und Verbrauchsdaten
- Benutzerkontrolle und Freigaben
- Integration Layer für steuerbare Systeme

### Ergebnis

Optimierungsvorschläge und Lastmanagement-Aktionen mit nachvollziehbarem Nutzen.

## Phase 8 - P2P Energy Trading

### Ziel

P2P-/EEG- und Negativstrompreis-Management zur Nutzung von Marktchancen und Vermeidung negativer Erlöse.

### Beschreibung

Die Präsentation beschreibt P2P/Negativstrompreis-Management mit Erzeugungsdaten, Marktpreisen, Wetter, Verbrauch, Speichern, P2P-/EEG-Netzwerk, Tarifen und Regulatorik. Entscheidungen bleiben Business-Module-Logik und nutzen Data Products.

### Abhängigkeiten

- Forecasting
- Marktpreis- und EEG/P2P-Datenquellen
- Rollen, Freigaben, Audit
- Integrationsadapter für externe Markt-/Community-Systeme

### Ergebnis

Handlungsstrategien für Speicher, Verbrauch, Lieferung, Handel und Abregelung.

## Phase 9 - KI & Optimierung

### Ziel

Optimierung, Lernen und Entscheidungsunterstützung über mehrere Business Modules hinweg.

### Beschreibung

KI nutzt Data Products, historische Ergebnisse, Monitoring-Abweichungen und Benutzerentscheidungen. Sie darf nicht die DLH-Grenzen umgehen. Modelle und Empfehlungen müssen versioniert, auditierbar und erklärbar genug für Betrieb und Freigabe sein.

### Abhängigkeiten

- Data Products und Forecasts
- Monitoring-Ergebnisse
- Qualitäts- und Modellmetadaten
- Benutzerfreigabe-Workflow

### Ergebnis

Optimierungsvorschläge, Prognosen, Diagnosemodelle und kontinuierliche Verbesserung.

## Phase 10 - ENSET Universe

### Ziel

Gesamtplattform aus Core Platform, Data Lake House, Integration Layer, Identity & Access Management, API Layer, Data Space und Business Modules.

### Beschreibung

Das ENSET Universe bedient Gemeinden, Unternehmen, Energiegemeinschaften, Forschung, Planungsbüros und Betreiber. Business Modules umfassen laut Präsentation unter anderem Municipal Building Platform, Energy Management, Energy Communities, Funding Management, Benchmarking, Reporting, ESG Management und CO2 Management.

### Abhängigkeiten

- Stabile Core Platform
- Data Products als verbindliche Konsumgrenze
- Data Space für freigegebene Data Products
- Administration, Benutzerverwaltung, Monitoring, Logging, Deployment und Governance

### Ergebnis

Skalierbare Energy Management Plattform mit modularen Business-Funktionen, DataProduct-Verbrauch und kontrolliertem Datenaustausch.
