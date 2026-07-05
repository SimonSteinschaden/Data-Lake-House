Welcome to the Data-Lake-House wiki!

Dieses Repository enthält die aktuelle Backend-Implementierung des ENSET Data Lake House.
Der Schwerpunkt liegt auf dem Domänenmodell, der Import-Pipeline, der Persistenz und einem Entwickler-Test-Harness.

Die wichtigsten Dokumente in diesem Wiki:
- `01_Architecture.md` – Architekturübersicht und geplante Komponenten
- `02_Data_Model.md` – Datenmodell und Felddefinitionen
- `04_Import.md` – Import- und Mapping-Architektur
- `05_Backend.md` – Backend-Struktur und Implementierungsdetails
- `06_API.md` – API-Status und Implementierungslücke
- `07_Frontend.md` – Frontend-Status
- `08_Data_Model.md` – Datenmodell und Entity-Beziehungen
- `11_Roadmap.md` – nächster Entwicklungsfahrplan

Aktuell fehlen im Repository:
- produktive ASP.NET Core API-Endpunkte
- Web- oder Desktop-Frontend
- orchestrierter ETL-/Worker-Lauf
- vollständige Persistenz für ImportJob/DataSource
- produktive Data Marketplace-Funktionalität

Die vorhandenen Projekte sind:
- `Enset.Domain`
- `Enset.Application`
- `Enset.Infrastructure`
- `Enset.Worker.Import` (Entwickler-/Test-Harness)

