# ENSET Architecture Baseline 1.0 RC

Stand: 20. Juli 2026. Diese Baseline beschreibt ausschließlich den im Repository vorhandenen Code. Historische Reviews bleiben Entscheidungsprotokolle; bei Abweichungen ist dieses Dokument zusammen mit `ARCHITECTURE_REVIEW_V1_0_RC.md` maßgeblich.

## Systemgrenzen

ENSET besteht aus sechs Projekten. `Enset.Domain` enthält Entities und Enums. `Enset.Application` enthält Use Cases und Ports und referenziert nur Domain. `Enset.Infrastructure` implementiert Ports mit EF Core, Excel/CSV und Filesystem. `Enset.Api` und `Enset.Worker` sind getrennte Hosts. `Enset.Web` ist eine eigenständig buildbare React/Vite-Anwendung.

```text
Web ─REST─> Api ─> Application ─> Domain
              └─> Infrastructure ─> PostgreSQL / Raw-Zone-Filesystem
Worker ──────────> Application + Infrastructure
```

Die Projektreferenzen sind azyklisch. Infrastructure referenziert Application und Domain; Application kennt Infrastructure nicht.

## Implementierte fachliche Bereiche

- Kunden, Projekte, Gebäude und Gebäudeversionen
- Geography-Stammdaten und normalisierte Adressen
- Energiesysteme, Zähler und Messwerte
- Energiegemeinschaften und Dokumente als persistierbare Grundmodelle
- Importanalyse, Issues, Resolutions, Write Gate, Reports und Audit
- Data Product Engine mit zwei Generatoren, Versionierung, Availability und Authorization
- REST-Endpunkte für Import und Data Products
- React Import Wizard und Data-Product-Dashboard

Marketplace, Mobility, Subscriptions, frei definierte Aggregationsgruppen und mehrere Analytics-Typen sind keine vollständigen End-to-End-Module.

## Persistenz

`EnsetDbContext` verwendet PostgreSQL über Npgsql. Die Migrationen `InitialPhase3Model` und `AddDataProductVerticalSlice` bilden den aktuellen Snapshot. Der DataProduct-Slice besitzt einen Unique Index auf `(DataProductId, VersionNumber)`. TimescaleDB oder getrennte physische Bronze/Silver/Gold-Speicher sind nicht implementiert.

Die Raw Zone ist durch `FileSystemRawZoneWriter` realisiert. Die qualitätsgesicherten relationalen Tabellen bilden den derzeitigen Data-Lake-Fachbestand. Silver und Gold sind fachliche Ebenen, keine separaten technischen Zonen.

## Data Product Engine

`DataProductDefinition` beschreibt den Produkttyp. `DataProduct` ist eine konkrete Instanz mit genau einem für die Generierung akzeptierten `DataProductScopeAssignment`. `DataProductCustomerAssignment` steuert die MVP-Berechtigung. `DataProductGenerationService` führt Authorization, Scope-Prüfung, Availability, Generatorauflösung, Run-Erzeugung, Berechnung und Versionierung aus.

Generatoren greifen ausschließlich über `IMeterReadingDataReader` und `IBuildingDataReader` auf scopebezogene Data-Lake-Daten zu. `EfDataProductReader` ist der EF-Adapter. CSV, Excel und Uploads sind Ingestion-Wege und für Generatoren unsichtbar.

Implementiert sind `METER_CONSUMPTION_SUMMARY` und `BUILDING_ENERGY_PROFILE`. `MeterReading.Value` wird über ReadingType, Quantity, Unit, Direction, IntervalSeconds und QualityFlag interpretiert.

## Import Engine

Die API unterstützt Analyse, Reportabruf, Resolution und Commit. Excel-Reader, Mapper, Validator, Duplication Check, Write Gate, Excel Writer, Raw-Zone-Writer und Report-Persistenz sind vorhanden. `DatabaseImportWriter` bricht bewusst mit `NotSupportedException` ab; ein relationaler Import-Commit ist somit noch nicht produktiv.

Der Worker führt denselben Analysepfad als Console-Anwendung aus, verwendet aktuell jedoch einen fest codierten lokalen Workbook-Pfad.

## API und Web

Implementiert sind die Endpunkte unter `/api/v1/imports` und `/api/v1/data-products`. Controller liefern DTOs und ProblemDetails. Eine echte Authentifizierung fehlt; `X-User-Id` ist eine MVP-Konvention.

React implementiert Import Wizard, Data-Product-Liste, Detail/Generate-Flow, Latest Version, Versionshistorie und Building Energy Dashboard. Kunden-, Gebäude-, Analytics- und Settings-Seiten sind derzeit einfache Seitengerüste.

## Verbindliche Diagramme

Die Ist-Architektur ist in [UML](UML/) dokumentiert. Die Diagramme sind Bestandteil dieser Baseline.
