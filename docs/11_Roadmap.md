# Roadmap

## Kurzfristige Aufgaben

- Import-Service vervollständigen:
  - Validierung implementieren
  - Mapping von `MeterReadingImportDto` auf `MeterReading` realisieren
  - Persistenz in `EnsetDbContext` implementieren
- DbContext erweitern:
  - `ImportJob` als `DbSet`
  - `DataSource` als `DbSet`
- `Enset.Worker.Import` in ein produktives Worker-Projekt überführen
- API-Projekt hinzufügen und erste Endpunkte entwickeln

## Mittelfristige Aufgaben

- Frontend-Prototyp erstellen (WinUI oder Web)
- KPI-/Benchmark-Services um Rechenlogik erweitern
- Data Lake-Zonen konzeptionell umsetzen (Raw/Silver/Gold)
- Export-Services für Data Products entwickeln
- Authentifizierung/Autorisierung für API und Frontend planen

## Langfristige Vision

- Produktiver Data Marketplace
- Anonyme Benchmark- und Verbrauchsdatensätze anbieten
- Externe Datenquellen anbinden (CRM, Sensoren, API)
- Automatisierte ETL-Pipeline und Scheduling
- Skalierbare Cloud- oder Hybrid-Architektur

## Verbesserungsvorschläge der Architektur und Prompting für Code-Optimierung

- Analyse des bisherig Erarbeiteten 
- Vorschläge zur Verbesserung für Skalierbarkeit des Projekts
- Prompts für Generalüberarbeitung des Codes