```plantuml
# Import-Architektur

Dieses Dokument beschreibt die geplante Import-, Mapping- und Exportarchitektur.
Aktuell ist im Repository eine Import-Pipeline mit DTOs, Abstraktionen und konkreter Infrastruktur vorhanden.

@startuml
title ENSET Import-, Mapping- und Exportarchitektur

actor "Interner Nutzer" as User
actor "Zoho CRM" as Zoho
actor "XML Datei" as XmlFile
actor "Excel/XLSM Datei" as ExcelFile
actor "Externer Käufer\nData Product" as Buyer

package "Input Sources" {
  [XML Import File] as XML
  [Excel/XLSM Import File] as Excel
  [Zoho CRM API] as ZohoApi
}

package "Connector Layer" {
  [XmlImportConnector] as XmlConnector
  [ExcelImportConnector] as ExcelConnector
  [ZohoCrmConnector] as ZohoConnector
}

package "Mapping Layer" {
  [External DTOs] as DTOs
  [Field Mapping Service] as Mapping
  [Unit Normalization Service] as UnitNorm
  [Validation Service] as Validation
}

package "Domain Model" {
  [Customer]
  [Project]
  [Region]
  [Municipality]
  [District]
  [Building]
  [EnergySystem]
  [Meter]
  [MeterReading]
  [Document]
  [ImportJob]
  [DataSource]
}

package "Storage Layer" {
  database "PostgreSQL\noperative Daten" as PostgreSQL
  database "TimescaleDB\nZeitreihen" as Timescale
  folder "Raw Zone\nOriginal XML/XLSM/API Payloads" as Raw
  folder "Silver Zone\nvalidierte Daten" as Silver
  folder "Gold Zone\nKPIs / Benchmarks" as Gold
}

package "Data Product Layer" {
  [Anonymization Service] as Anon
  [Aggregation Service] as Agg
  [Data Product Service] as DataProduct
  [Export Service] as Export
}

package "Output Formats" {
  [XML Export] as XmlExport
  [CSV Export] as CsvExport
  [JSON/API Export] as JsonExport
}

User --> XML
User --> Excel
Zoho --> ZohoApi

XML --> XmlConnector
Excel --> ExcelConnector
ZohoApi --> ZohoConnector

XmlConnector --> Raw
ExcelConnector --> Raw
ZohoConnector --> Raw

XmlConnector --> DTOs
ExcelConnector --> DTOs
ZohoConnector --> DTOs

DTOs --> Mapping
Mapping --> UnitNorm
UnitNorm --> Validation

Validation --> Customer
Validation --> Project
Validation --> Region
Validation --> Municipality
Validation --> District
Validation --> Building
Validation --> EnergySystem
Validation --> Meter
Validation --> MeterReading
Validation --> Document
Validation --> ImportJob
Validation --> DataSource

Customer --> PostgreSQL
Project --> PostgreSQL
Region --> PostgreSQL
Municipality --> PostgreSQL
District --> PostgreSQL
Building --> PostgreSQL
EnergySystem --> PostgreSQL
Meter --> PostgreSQL
MeterReading --> Timescale
Document --> PostgreSQL
ImportJob --> PostgreSQL
DataSource --> PostgreSQL

PostgreSQL --> Silver
Timescale --> Silver
Silver --> Gold

Gold --> Anon
Anon --> Agg
Agg --> DataProduct
DataProduct --> Export

Export --> XmlExport
Export --> CsvExport
Export --> JsonExport

XmlExport --> User
CsvExport --> Buyer
JsonExport --> Buyer

@enduml
```

# Aktuelle Import-Architektur

## Verantwortlichkeiten nach Layer

- src/Enset.Domain/ enthält das reine Domain-Modell (Meter, MeterReading, Building, Project, Customer, usw.).
- src/Enset.Application/ enthält Import-DTOs, Enums, Models und Abstraktionen.
- src/Enset.Infrastructure/ enthält den EF Core EnsetDbContext, konkrete Reader-/Mapper-Implementierungen und Services.

## Relevante Dateien

- src/Enset.Application/Imports/DTOs/MeterImportDto.cs
- src/Enset.Application/Imports/DTOs/MeterReadingImportDto.cs
- src/Enset.Application/Imports/Abstractions/IMeterReadingReader.cs
- src/Enset.Application/Imports/Abstractions/IMeterReadingReaderFactory.cs
- src/Enset.Application/Imports/Abstractions/IMeterLookupService.cs
- src/Enset.Application/Imports/Abstractions/IMeterReadingMapper.cs
- src/Enset.Application/Imports/Enums/ImportSourceType.cs
- src/Enset.Application/Imports/Enums/ImportStatus.cs
- src/Enset.Application/Imports/Enums/RawDataObjectType.cs
- src/Enset.Application/Imports/Models/ImportJob.cs
- src/Enset.Application/Imports/Models/RawDataObject.cs

- src/Enset.Infrastructure/DBContext.cs
- src/Enset.Infrastructure/Imports/CsvMeterReadingReader.cs
- src/Enset.Infrastructure/Imports/Factory/MeterReadingReaderFactory.cs
- src/Enset.Infrastructure/Imports/Services/MeterLookupService.cs
- src/Enset.Infrastructure/Imports/Mapping/MeterReadingMapper.cs

## Architekturelle Fakten

- MeterReading ist ein Domain-Zeitreihenobjekt und erbt nicht von BaseEntity.
- Meter erbt von BaseEntity und nutzt MeterNumber als fachliche Identität.
- MeterId bleibt die technische interne GUID.
- Importdateien dürfen MeterNumber verwenden, nicht aber die interne MeterId.
- EnsetDbContext konfiguriert den Composite Key MeterId + Timestamp für MeterReading.
- ImportJob und DataSource sind aktuell nicht Teil eines DbSet in EnsetDbContext.

## Laufender Zustand

- Build der drei Projekte ist möglich.
- Enset.Domain, Enset.Application und Enset.Infrastructure sind aktuell fehlerfrei kompilierbar.
