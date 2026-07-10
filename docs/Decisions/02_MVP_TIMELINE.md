# ENSET Data Lake House MVP - Zeitplan

Ziel: MVP Version 1.0 spätestens Mitte August abschließen. Ausgangspunkt ist der aktuelle Repository-Stand mit funktionierendem Import-Analysepfad, REST API, Resolution/Commit-Gate, JSON-Reportpersistenz, Excel-Adaptern, Raw-Zone-Writer, EF-Core-Modell und ersten Architekturtests.

Planungsannahme: Umsetzung ab Kalenderwoche 29/2026. Mitte August entspricht Kalenderwoche 33/2026. Der Plan priorisiert MUST-Aufgaben und verschiebt SHOULD/COULD nur dann in das MVP, wenn sie technische Voraussetzung sind.

## Kalenderwoche 29

### Ziele

- MVP-Schnittstellen stabilisieren.
- Vollständigen fachlichen Importumfang festlegen.
- Produktiven DB-Schreibpfad vorbereiten.

### Aufgaben

- [ ] DataProduct Contract v1 definieren: Scope, Version, Status, Qualität, Provenance, Gültigkeit.
- [ ] Application Ports `IDataProductPublisher` und `IDataProductQueryPort` ergänzen.
- [ ] Building-, Meter- und MeterReading-Mapping gegen bestehende Excel-Struktur vervollständigen.
- [ ] Validierungsregeln für Building, Meter und MeterReading strukturieren.
- [ ] ImportJob/DataSource-Konzept für Persistenz und Provenance finalisieren.
- [ ] API Request-/Response-DTOs einfrieren und OpenAPI-Basis prüfen.

### Meilenstein

- Architekturkonforme Import- und DataProduct-Verträge stehen fest.

### Risiken

- Excel-Struktur kann fachliche Sonderfälle enthalten, die aktuell nur teilweise modelliert sind.
- DataProduct Contract darf nicht zu groß werden, weil MVP Mitte August erreicht werden muss.

## Kalenderwoche 30

### Ziele

- Ende-zu-Ende-Import in die Datenbank implementieren.
- Curated/Silver-Pfad aufbauen.
- TimescaleDB-Entscheidung technisch nachweisen.

### Aufgaben

- [ ] `DatabaseImportWriter` transaktional implementieren.
- [ ] Upsert-/Insert-Regeln für Customer, Project, Building, Meter und MeterReading festlegen und testen.
- [ ] ImportJob, DataSource und ImportHistory persistieren.
- [ ] Curated Write Pipeline mit Provenance verknüpfen.
- [ ] PostgreSQL/TimescaleDB-Migrationen prüfen und Hypertable-Strategie umsetzen oder MVP-Entscheidung dokumentieren.
- [ ] Datenbank-Integrationstest-Grundlage mit Testcontainer/Compose vorbereiten.

### Meilenstein

- Validierter Import kann nach Benutzerfreigabe in die DLH-Persistenz geschrieben werden.

### Risiken

- Transaktionale Upserts für Zeitreihen können bei Dubletten- und Idempotenzregeln mehr Abstimmung erfordern.
- TimescaleDB-Setup kann Deployment- und Migrationsaufwand erhöhen.

## Kalenderwoche 31

### Ziele

- REST API produktionsnäher absichern.
- React Import Wizard beginnen und an echte API anbinden.
- Security-Mindeststandard integrieren.

### Aufgaben

- [ ] JWT Bearer/OIDC-Authentifizierung in der API integrieren.
- [ ] Rollen für Analyse, Resolution und Commit technisch abbilden.
- [ ] Einheitliche ProblemDetails, Upload-Limits und Content-Type-Prüfung ergänzen.
- [ ] OpenAPI-Dokumentation für Importworkflow finalisieren.
- [ ] React-Projekt aufsetzen.
- [ ] Upload-, Analyze- und Reportanzeige implementieren.
- [ ] Issue-Gruppierung und ResolutionActions im UI erfassen.

### Meilenstein

- Benutzer können einen Import über die Web UI analysieren und Issues bearbeiten.

### Risiken

- Identity Provider kann lokal und containerisiert zusätzliche Konfiguration benötigen.
- UI-Implementierung hängt von stabilen API-Fehlerverträgen ab.

## Kalenderwoche 32

### Ziele

- Commit, Raw Zone und DataProduct MVP über UI abschließen.
- Deployment und CI stabilisieren.
- Testabdeckung auf MVP-Flows erweitern.

### Aufgaben

- [ ] Commit-Schritt im React Import Wizard implementieren.
- [ ] ImportHistory- und Statusanzeige im UI mindestens für laufende/offene/abgeschlossene Imports ergänzen.
- [ ] Raw-Zone-Betriebspfad, Retention-Grundregel, Prüfsumme und Zugriffsschutz konfigurieren.
- [ ] MVP Data Product "Objektprofil" erzeugen und über Query Port bereitstellen.
- [ ] DataProduct-Status im API-/UI-Pfad anzeigen.
- [ ] Dockerfile und Docker Compose für API, DB und optionale Auth-Komponente erstellen.
- [ ] CI mit Restore, Build, Test und Architekturtests einrichten.
- [ ] API-Integrationstests und DB-Integrationstests ergänzen.

### Meilenstein

- Ein kompletter Import läuft über UI und API bis in DLH/Curated/DataProduct-Grundlage.

### Risiken

- Frontend, Auth, DB und Containerisierung laufen parallel und müssen eng integriert werden.
- MVP DataProduct muss bewusst minimal bleiben, ohne die Data-Product-Grenze zu verwässern.

## Kalenderwoche 33

### Ziele

- MVP stabilisieren.
- Abschlussreview gegen Architecture Baseline v1.0 durchführen.
- Dokumentation und Releasefähigkeit herstellen.

### Aufgaben

- [ ] End-to-End-Test für Upload, Analyze, Resolution, Commit, Raw Archive und DataProduct-Abfrage abschließen.
- [ ] Security-Testfälle für Rollen, fehlende Tokens und unzulässige Commit-Versuche ergänzen.
- [ ] Logging, Health Checks und Basis-Monitoring aktivieren.
- [ ] Betriebsdokumentation für Konfiguration, Docker, Migrationen und Troubleshooting schreiben.
- [ ] API-Dokumentation und OpenAPI-Vertrag veröffentlichen.
- [ ] Abschlussreview gegen Baseline v1.0 dokumentieren.
- [ ] MVP Version 1.0 tag- und releasefähig machen.

### Meilenstein

- MVP Version 1.0 ist bis Mitte August abgeschlossen.

### Risiken

- Späte Integrationsfehler in Auth/DB/UI können Release-Puffer verbrauchen.
- Offene SHOULD-Aufgaben müssen konsequent aus dem MVP herausgehalten werden, wenn sie nicht releasekritisch sind.

# MVP Version 1.0

## Enthaltene Features

- [x] Clean-Architecture-Schichten: Domain, Application, Infrastructure, API, Worker.
- [x] REST API als reine Kommunikationsschicht.
- [x] Importanalyse über Application Use Case.
- [x] Strukturierte ImportReports, Issues, Decisions und Audit Trail.
- [x] Benutzerentscheidung vor schreibendem Commit.
- [x] Write Gate für alle Schreibzugriffe.
- [x] Excel Reader und Excel Writer als Infrastructure Adapter.
- [x] Raw-Zone-Archivierung der Originaldatei.
- [ ] Vollständiger Building-, Meter- und MeterReading-Import.
- [ ] Transaktionaler DatabaseImportWriter.
- [ ] Persistente ImportHistory mit Provenance.
- [ ] Curated/Silver-Schreibpfad.
- [ ] Minimaler versionierter DataProduct Contract.
- [ ] DataProduct Publisher und Query Port.
- [ ] MVP Data Product "Objektprofil".
- [ ] React Import Wizard.
- [ ] Authentifizierung und rollenbasierte Autorisierung.
- [ ] OpenAPI-Vertrag und stabile Fehlerverträge.
- [ ] Docker/Compose für reproduzierbaren Betrieb.
- [ ] CI mit Build, Tests und Architekturprüfungen.
- [ ] API-, DB-, Security- und E2E-MVP-Tests.
- [ ] Betriebs- und Abschlussdokumentation.

## Nicht Bestandteil von MVP 1.0

- Vollständige Business Modules wie Energy Management, Benchmarking, Monitoring, Forecasting, Load Management oder P2P Trading.
- Data Space / Data Marketplace als produktive Austauschplattform.
- KI-Optimierung, automatische Steuerung oder Live-IoT-Streaming.
- Vollständige Administration und Benutzerverwaltung jenseits der für MVP nötigen Rollen und Auth-Grenzen.
