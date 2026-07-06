# Dokumentationsübersicht

Dieses Repository enthält den aktuellen Backend-Prototyp des ENSET Data Lake House. Die Dokumentation trennt verbindliche Zielvorgaben von der Beschreibung des tatsächlich implementierten Stands.

## Verbindliche Dokumente

- `Decisions/ARCHITECTURE_REVIEW_V1_0.md`: unveränderte Architecture Baseline v1.0
- `Decisions/ARCHITECTURE_REVIEW_V1_1.md`: aktueller IST-Review und Abweichungen zur Baseline

Version 1.1 ersetzt oder verändert die Baseline nicht.

## Fach- und Entwicklerdokumente

- `01_Architecture.md`: Layer, Abhängigkeiten und aktuelle Komponenten
- `02_Data_Model.md`: fachliches Datenmodell
- `03_Data_Lake_House.md`: umgesetzte und offene Data-Lake-House-Bausteine
- `04_Import.md`: Importanalyse und vorbereiteter Resolution-/Write-Pfad
- `05_Backend.md`: Backend- und Projektstruktur
- `06_API.md`: API-Vorbereitung und offene Endpunkte
- `07_Frontend.md`: Status der Benutzeroberfläche
- `08_Data_Model.md`: technische Entity- und Persistenzsicht
- `09_KPIs.md`: KPI-Modell und Implementierungsstatus
- `10_Entities.md`: C#-Entities und Prozessmodelle
- `11_Roadmap.md`: verbleibende Meilensteine bis Version 1.0
- `Decisions/Todos.md`: konkrete technische TODOs

## Kurzstatus

Die aktive Importpipeline analysiert Excel-Daten und liefert einen `ImportReport`. Benutzerentscheidungen und Schreibfreigaben sind als Application-Bausteine vorhanden, aber noch nicht in einem produktiven API-/Worker-Ablauf integriert. API, UI, Background Jobs, Reportpersistenz, Importhistorie und Tests fehlen weiterhin.
