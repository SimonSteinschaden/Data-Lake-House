# Data Model

## Ziel
Das Datenmodell bildet die Grundlage für:
- Energieberatungsprojekte
- Gebäudestrukturen
- Energiedaten
- Benchmarking
- spätere Data Products

---

## Kernprinzip

Trennung von:
1. Stammdaten
2. Messdaten
3. Metadaten
4. Analyse-Daten

## Räumliches Ebenenmodell

Das Datenmodell muss Daten auf mehreren Ebenen speichern und auswerten können:

```text
Region
→ Municipality / Ort
→ District / Quartier
→ Building / (Betriebs-)Gebäude
→ EnergySystem / Meter
→ MeterReading
```

Ein Building ist das zentrale Objekt der Energieberatung. Es kann private, betriebliche oder öffentliche Gebäude abbilden, z. B. Wohnungen, Einfamilienhäuser, Mehrfamilienhäuser, Betriebe, Hallen, Schulen oder kommunale Gebäude.

---

## Haupt-Entities

### Customer
- Id (GUID)
- Name
- Type (Private, Company, Municipality)
- CreatedAt

---

### Project
- Id (GUID)
- CustomerId
- Name
- StartDate
- Status

---

### Building
- Id (GUID)
- ProjectId (GUID)
- DistrictId (GUID)
- Name
- PrimaryUseType (Residential, Commercial, Public, Mixed)
- BuildingCategory (Apartment, House, Office, Hall, School, Retail, Industry, Other)
- OwnershipType (OwnerOccupied, Rented, PublicOwned, CompanyOwned, Other)
- IsResidential
- IsCommercial
- IsPublic
- HasMixedUse
- YearOfConstruction
- FloorArea_m2

---

### EnergySystem
- Id (GUID)
- BuildingId
- Type (PV, Battery, Heating)
- Capacity
- InstallationYear

---

### Meter
- Id (GUID)
- BuildingId
- Type (Consumption, Production)
- Unit (kWh)
- ExternalId

---

### MeterReading (Timeseries!)
- Id (GUID)
- MeterId
- Timestamp
- Value
- Unit
- QualityFlag

---

### Document
- Id (GUID)
- ProjectId
- Type (Energieausweis, Rechnung)
- FilePath
- UploadedAt

---

### ImportJob
- Id (GUID)
- ProjectId
- SourceType (CSV, Excel, PDF, API)
- Status
- CreatedAt

---

### DataSource
- Id (GUID)
- Name
- Type (UserUpload, API, Sensor)

---

### CalculationResult
- Id (GUID)
- KPIType
- ScopeLevel (Region, Municipality, District, Building)
- ScopeId (GUID)
- Value
- Unit
- PeriodStart
- PeriodEnd
- CalculatedAt

---

### BenchmarkDataset
- Id (GUID)
- Category
- Region
- BuildingType
- BuildingCategory
- YearRange
- AvgConsumption
- SampleSize
- ScopeLevel

---

## Beziehungen

```plantuml
@startuml

entity Customer {
  Id : GUID
  Name
  Type
}

entity Project {
  Id : GUID
  CustomerId
  Name
  Status
}

entity Region {
  Id : GUID
  Name
}

entity Municipality {
  Id : GUID
  RegionId
  Name
}

entity District {
  Id : GUID
  MunicipalityId
  Name
}

entity Building {
  Id : GUID
  ProjectId
  DistrictId
  PrimaryUseType
  BuildingCategory
  OwnershipType
  YearOfConstruction
  FloorArea_m2
}

entity EnergySystem {
  Id : GUID
  BuildingId
  Type
  Capacity
}

entity Meter {
  Id : GUID
  BuildingId
  Type
  Unit
}

entity MeterReading {
  Id : GUID
  MeterId
  Timestamp
  Value
  QualityFlag
}

entity Document {
  Id : GUID
  ProjectId
  Type
  FilePath
}

entity ImportJob {
  Id : GUID
  ProjectId
  SourceType
  Status
}

entity CalculationResult {
  Id : GUID
  ProjectId
  KPIType
  Value
}

entity BenchmarkDataset {
  Id : GUID
  Region
  BuildingType
  AvgConsumption
  SampleSize
}



Customer ||--o{ Project
Project ||--o{ Building
District ||--o{ Building
Building ||--o{ EnergySystem
Building ||--o{ Meter
Meter ||--o{ MeterReading
Project ||--o{ Document
Project ||--o{ ImportJob
Project ||--o{ CalculationResult
BenchmarkDataset ..> Building
Region ||--o{ Municipality
Municipality ||--o{ District
@enduml
```

---

## Zeitreihenmodell (wichtig!)

```plantuml
@startuml

Meter --> MeterReading
MeterReading --> DataSource

@enduml
```

👉 TimescaleDB empfohlen

---

## Datenzonen (Lake House)

- Raw: Originaldateien
- Silver: validierte Daten
- Gold: KPIs / Benchmarks

---

## Designentscheidungen

- GUIDs statt Integer (besser für Skalierung)
- Trennung von Meter und MeterReading
- flexible EnergySystem Struktur
- Benchmark als eigenes Dataset

---

## Nächste Schritte

- EF Core Entities erstellen
- Migration generieren
- erste Testdaten einfügen
- Importpipeline anbinden