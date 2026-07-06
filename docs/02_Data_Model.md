# Fachliches Datenmodell

Dieses Dokument beschreibt das aktuell im Domain-Projekt vorhandene Modell. Persistenzdetails stehen in `08_Data_Model.md`.

## Ebenenmodell

```text
Region
  -> Municipality
  -> District
  -> Building
  -> Meter
  -> MeterReading
```

Projekte verbinden Customers mit Buildings. Ein Building kann außerdem EnergySystems enthalten.

## Haupt-Entities

| Entity | Wesentliche Felder und Beziehungen |
|---|---|
| `Customer` | `Name`, `CustomerType`, mehrere Projects |
| `Project` | `CustomerId`, `Name`, `StartDate`, `Status`, Buildings, Documents |
| `Region` | `Name`, Municipalities |
| `Municipality` | `RegionId`, `Name`, `ZipCode`, Districts |
| `District` | `MunicipalityId`, `Name`, Buildings |
| `Building` | `ProjectId`, `DistrictId`, Nutzung, Kategorie, Eigentum, Baujahr, Fläche, EnergySystems, Meters |
| `EnergySystem` | `BuildingId`, Typ, Leistung, Installationsjahr |
| `Meter` | optionale `BuildingId`, eindeutige `MeterNumber`, Typ, Einheit, ExternalId, Readings |
| `MeterReading` | `MeterId`, `Timestamp`, Wert, Einheit, QualityFlag und optionale Provenance-IDs |
| `Document` | `ProjectId`, Typ, Dateipfad, Uploadzeitpunkt |
| `CalculationResult` | KPI, Scope, Wert, Einheit und Zeitraum |
| `BenchmarkDataset` | Scope, Region, Gebäudekategorie, Zeitraum, Durchschnitt und Stichprobe |
| `DataSource` | Name und Quellentyp |

Fast alle Domain-Entities erben von `BaseEntity` mit GUID, `CreatedAt` und optionalem `UpdatedAt`. `MeterReading` ist bewusst kein `BaseEntity`; sein Schlüssel besteht aus `MeterId + Timestamp`.

## Importspezifische Modelle

`ImportJob` und `RawDataObject` liegen derzeit in `Enset.Application`, nicht in Domain. `ImportJob` ist noch nicht als aktives DbSet registriert. `RawDataObject` ist lediglich ein Modell; ein Raw-Zone-Store ist nicht implementiert.

## Nicht implementierte Aspekte

- keine persistente ImportReport-/ImportHistory-Beziehung;
- keine technisch umgesetzten Raw-/Curated-/Data-Product-Modelle;
- keine Data-Product-Verträge oder Provenance-Kette;
- keine vollständige Importabbildung auf alle Domain-Entities.
