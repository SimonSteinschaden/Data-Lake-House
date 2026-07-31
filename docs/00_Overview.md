# Dokumentationsübersicht

Dieses Repository enthält den aktuellen Backend-Prototyp des ENSET Data Lake House. Die Dokumentation trennt verbindliche Zielvorgaben von der Beschreibung des tatsächlich implementierten Stands.

## Verbindliche Dokumente

- `Decisions/ARCHITECTURE_REVIEW_V1_0.md`: unveränderte Architecture Baseline v1.0
- `Decisions/ARCHITECTURE_REVIEW_V1_1.md`: historischer IST-Review nach Phase 1
- `Decisions/ARCHITECTURE_REVIEW_V1_2.md`: aktueller IST-Review nach Phase 2

Die Reviews v1.1 und v1.2 ersetzen oder verändern die Baseline nicht.

## Fach- und Entwicklerdokumente

- `01_Architecture.md`: Layer, Abhängigkeiten und aktuelle Komponenten
- `02_Data_Model.md`: fachliches Datenmodell
- `03_Data_Lake_House.md`: umgesetzte und offene Data-Lake-House-Bausteine
- `04_Import.md`: Importanalyse und vorbereiteter Resolution-/Write-Pfad
- `05_Backend.md`: Backend- und Projektstruktur
- `06_API.md`: implementierte Import-API und offene API-Querschnittsthemen
- `07_Frontend.md`: Status der Benutzeroberfläche
- `08_Data_Model.md`: technische Entity- und Persistenzsicht
- `09_KPIs.md`: KPI-Modell und Implementierungsstatus
- `10_Entities.md`: C#-Entities und Prozessmodelle
- `11_Roadmap.md`: verbleibende Meilensteine bis Version 1.0
- `12_Phase_3_Data_Platform.md`: verbindlicher technischer Blueprint für PostgreSQL, TimescaleDB, operative APIs, Historisierung, KPI, Reporting, Monitoring und Phase-4-Sicherheitsvorbereitung
- `Decisions/Todos.md`: konkrete technische TODOs

## Kurzstatus

Die Importpipeline ist zweistufig: Analyse erzeugt und persistiert einen `ImportReport`; Resolution und Commit laufen über denselben Application-Pfad für API und Console-Test-Runner. Die REST-Endpunkte und dateibasierte Report-/Raw-Persistenz sind vorhanden. UI, Authentifizierung, Background Jobs, fachliches Database-Mapping und eine datenbankgestützte Importhistorie fehlen weiterhin.
# Architecture Freeze 1.0 RC

Der verbindliche Ist-Stand ist in [ARCHITECTURE_BASELINE_V1_0_RC.md](ARCHITECTURE_BASELINE_V1_0_RC.md) beschrieben. Offene Punkte stehen ausschließlich in [IMPLEMENTATION_ROADMAP_V1_0_RC.md](IMPLEMENTATION_ROADMAP_V1_0_RC.md). Dieses Dokument enthält ergänzende Produktübersicht und kann historische Zielbilder enthalten.
