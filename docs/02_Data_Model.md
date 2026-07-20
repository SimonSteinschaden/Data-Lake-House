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

`ImportJob` und `RawDataObject` liegen derzeit in `Enset.Application`, nicht in Domain. `ImportJob` ist noch nicht als aktives DbSet registriert. Die Raw-Zone-Ablage erfolgt aktuell dateibasiert über `FileSystemRawZoneWriter`; `RawDataObject` ist damit noch nicht persistent verknüpft.

## Nicht implementierte Aspekte

- keine datenbankgestützte ImportReport-/ImportHistory-Beziehung;
- kein persistentes Raw-/Curated-/Data-Product-Domänenmodell;
- keine Data-Product-Verträge oder Provenance-Kette;
- keine vollständige Importabbildung auf alle Domain-Entities.
# Stand 1.0 RC

Das verbindliche persistierte Modell ergibt sich aus `EnsetDbContext`, den EF-Konfigurationen und dem Snapshot. Das aktuelle ER-Diagramm liegt in [10_Database_ER.puml](UML/10_Database_ER.puml). Nicht im DbContext oder Snapshot enthaltene Konzepte sind nicht als persistiert zu betrachten.
