# Architekturüberblick

Die aktuelle Repository-Implementierung umfasst den Backend-Kern mit Domain, Application und Infrastructure.
Zu den vorhandenen Projekten gehört zudem `Enset.Worker.Import` als Entwicklungs- und Test-Harness.

> Hinweis: Die folgenden PlantUML-Diagramme zeigen die Zielarchitektur. Im Vergleich dazu beschreibt der Abschnitt "Aktueller Implementierungsstand" den aktuellen Codezustand.

## Aktueller Implementierungsstand

- `Enset.Domain`: vollständig als reines Domainmodell vorhanden
- `Enset.Application`: Import-bezogene DTOs, Abstraktionen, Enums und Prozessmodelle
- `Enset.Infrastructure`: EF Core DbContext, Persistenz, Import-/Export-Reader, Service-Grundgerüste
- `Enset.Worker.Import`: Entwickler-Harness für CSV-/Excel-Importtests, Validierung und Export

## Noch nicht implementiert

- ASP.NET Core Web API mit Controllern / Endpunkten
- Produktives Frontend (Web oder Desktop)
- Vollständiger Import-/Mapping-/Persistenz-Workflow
- Orchestrierter ETL-Worker im Produktivbetrieb
- Data Marketplace und Data Product Layer

## Zielarchitektur

Die PlantUML-Diagramme beschreiben die langfristige Zielarchitektur mit API, UI, Worker-Schichten und Storage-Zonen.

```plantuml

'--------------------------------------------------------------------------------------
'UML COMPONENTS DIAGRAM
'--------------------------------------------------------------------------------------
@startuml
title ENSET Data Lake House MVP - Komponentenarchitektur

actor "Interner Nutzer\nEnergieberatung" as User

package "Application Layer" {
  [WinUI3 Desktop MVP] as WinUI
  [Später: React/Next.js Portal] as WebUI
}

package "Access Layer" {
  [ASP.NET Core Web API] as API
  [OpenAPI / Swagger] as Swagger
}

package "Application / Domain Layer" {
  [Project Service] as ProjectService
  [Building Service] as BuildingService
  [Import Service] as ImportService
  [Calculation Service] as CalcService
  [Benchmark Service] as BenchmarkService
  [Data Product Service] as DataProductService
  [Validation Service] as ValidationService
}

package "Ingestion / Worker Layer" {
  [CSV Import Worker] as CsvWorker
  [Excel Import Worker] as ExcelWorker
  [PDF Intake Worker] as PdfWorker
  [Scheduled ETL Worker] as EtlWorker
}

package "Storage Layer" {
  database "PostgreSQL\nStammdaten, Projekte,\nGebäude, Metadaten" as PostgreSQL
  database "TimescaleDB\nZeitreihen / Messwerte" as Timescale
  folder "Raw Zone\nOriginaldateien" as Raw
  folder "Curated Zone\nvalidierte Daten" as Curated
  folder "Data Products\nstandardisierte Ergebnisse" as Products
}

package "Infrastructure" {
  [Docker Compose] as Docker
  [Synology Backup] as Synology
  [Azure Data Lake Gen2\nspäter skalierbar] as AzureLake
  [Azure Blob Backup] as AzureBackup
}

User --> WinUI
User --> WebUI

WinUI --> API
WebUI --> API
API --> Swagger

API --> ProjectService
API --> BuildingService
API --> ImportService
API --> CalcService
API --> BenchmarkService
API --> DataProductService

ImportService --> ValidationService
CsvWorker --> ImportService
ExcelWorker --> ImportService
PdfWorker --> ImportService
EtlWorker --> CalcService

ProjectService --> PostgreSQL
BuildingService --> PostgreSQL

ImportService --> PostgreSQL
ImportService --> Timescale
ImportService --> Raw

CalcService --> Timescale
CalcService --> Curated

BenchmarkService --> Curated
BenchmarkService --> PostgreSQL

Curated --> DataProductService
BenchmarkService --> DataProductService

DataProductService --> PostgreSQL
DataProductService --> Products
DataProductService --> API

Raw --> AzureLake
Curated --> AzureLake
Products --> AzureLake
PostgreSQL --> AzureLake
Timescale --> AzureLake

PostgreSQL --> Synology
Timescale --> Synology
Synology --> AzureBackup

Docker .. API
Docker .. CsvWorker
Docker .. ExcelWorker
Docker .. PdfWorker
Docker .. PostgreSQL
Docker .. Timescale

@enduml
```
'--------------------------------------------------------------------------------------
'DATA FLOW
'--------------------------------------------------------------------------------------
@startuml
title ENSET Data Lake House MVP - Datenfluss

actor "Energieberater" as User

participant "UI\nWinUI3/Blazor/React" as UI
participant "ASP.NET Core API" as API
participant "Import Service" as Import
participant "Validation Service" as Validation
database "PostgreSQL" as PG
database "TimescaleDB" as TS
collections "Raw Zone" as Raw
collections "Curated Zone" as Curated
participant "Calculation Service" as Calc
participant "Benchmark Service" as Bench
participant "Data Product Service" as Product
collections "Data Products" as Products

User -> UI: Projekt/Gebäude anlegen
UI -> API: POST /projects, /buildings
API -> PG: Stammdaten speichern

User -> UI: Datei importieren
UI -> API: POST /imports/files
API -> Import: ImportJob erstellen
Import -> Raw: Originaldatei speichern
Import -> Validation: Daten prüfen und normalisieren

Validation -> PG: Metadaten speichern
Validation -> TS: Messwerte speichern

Calc -> TS: Messdaten lesen
Calc -> PG: Gebäudedaten lesen
Calc -> Curated: Kennzahlen speichern

Bench -> Curated: Kennzahlen lesen
Bench -> PG: Vergleichsgruppen lesen

Curated -> Product: Datenbasis bereitstellen
Bench -> Product: Benchmark-Ergebnisse bereitstellen
Product -> Products: Standardisierte Data Products erzeugen

Products -> API: Data Products bereitstellen
API -> UI: Dashboarddaten / Auswertungen
UI -> User: Auswertung anzeigen

@enduml

'--------------------------------------------------------------------------------------
'DATA PRODUCT LAYER / MARKETPLACE EXTENSION
'--------------------------------------------------------------------------------------
@startuml
title ENSET Data Lake House - Data Product Layer und Marketplace-Erweiterung

package "Data Lake House" {
  [Raw Data]
  [Processed Data]
  [KPI / Benchmark Engine]
}

package "Data Product Layer" {
  [Quality Assurance]
  [Aggregation Service]
  [Anonymization Service]
  [Metadata Catalog]
  [Data Product Service]
  [Export Service]
  [Pricing Service]
}

package "Business Modules" {
  [Municipal Building Platform]
  [Energy Management]
  [Reporting]
  [Benchmarking]
}

package "Marketplace / Data Space Extension" {
  [Marketplace API]
  [Download / Purchase API]
  [Data Space Connector]
}

[Processed Data] --> [Quality Assurance]
[KPI / Benchmark Engine] --> [Quality Assurance]

[Quality Assurance] --> [Aggregation Service]
[Aggregation Service] --> [Anonymization Service]
[Anonymization Service] --> [Metadata Catalog]
[Metadata Catalog] --> [Data Product Service]

[Data Product Service] --> [Municipal Building Platform]
[Data Product Service] --> [Energy Management]
[Data Product Service] --> [Reporting]
[Data Product Service] --> [Benchmarking]

[Data Product Service] --> [Export Service]
[Data Product Service] --> [Pricing Service]

[Export Service] --> [Marketplace API]
[Marketplace API] --> [Download / Purchase API]
[Export Service] --> [Data Space Connector]

@enduml

# Aktuelle Projektstruktur

Die Implementierung wurde sauber nach Clean Architecture aufgeteilt:

- `src/Enset.Domain/`
  - Enthält ausschließlich Domain-Entities, Enums und reine Business-Logik.
  - Keine Abhängigkeit auf EF Core, Infrastructure oder Application.
  - Packages: `Common`, `Customers`, `Projects`, `Buildings`, `Energy`, `Documents`, `Analytics`, `Geography`, `Data`.

- `src/Enset.Application/`
  - Referenziert `Enset.Domain`.
  - Enthält Import-DTOs, Abstraktionen, Enums und Prozessmodelle.
  - Packages: `Imports/DTOs`, `Imports/Abstractions`, `Imports/Enums`, `Imports/Models`.

- `src/Enset.Infrastructure/`
  - Referenziert `Enset.Domain` und `Enset.Application`.
  - Enthält EF Core `EnsetDbContext`, TimescaleDB-/PostgreSQL-Persistenz, Reader-Implementierungen, Mapper-Implementierungen und konkrete Services.
  - Importlogik und Datenzugriff sind hier implementiert.

## Wichtige Punkte

- `MeterReading` ist ein Domain-Zeitreihenobjekt und erbt nicht von `BaseEntity`.
- `MeterReading` verwendet den Composite Key `MeterId + Timestamp`.
- `Meter` erbt von `BaseEntity` und verwendet `MeterNumber` als fachliche Identität.
- `MeterId` bleibt die technische interne GUID.
- Import-Dateien arbeiten mit `MeterNumber`, nicht mit der internen `MeterId`.
- `EnsetDbContext` liegt ausschließlich in `src/Enset.Infrastructure/DBContext.cs`.
- `ImportJob` und `DataSource` sind aktuell nicht als `DbSet` im DbContext enthalten.

## Architekturprinzip

Das ENSET Data Lake House dient nicht ausschließlich der Speicherung energierelevanter Daten.

Sein primärer Zweck besteht darin, aus unterschiedlichsten Datenquellen standardisierte, qualitätsgesicherte und wiederverwendbare Data Products bereitzustellen.

Business Modules wie die Municipal Building Platform, das Energy Management oder zukünftige Anwendungen greifen ausschließlich auf diese Data Products zu und nicht direkt auf Rohdaten oder interne Speicherstrukturen.

Dadurch werden Wiederverwendbarkeit, Konsistenz sowie eine klare Trennung zwischen Datenhaltung, Datenverarbeitung und Fachanwendungen gewährleistet.