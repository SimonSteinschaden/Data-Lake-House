# Data Lake House

## Ziel
Das Data Lake House soll strukturierte Energieberatungsdaten, Gebäudedaten und Zeitreihen zusammenführen.
Es ist als Basis für Analysen, Benchmarking und spätere Data Products gedacht.

## Aktueller Zustand

Im Repository sind aktuell nur die Backend-Schichten implementiert:
- Reines Domainmodell (`Enset.Domain`)
- Import-Abstraktionen, DTOs und Modelle (`Enset.Application`)
- EF Core-Persistenz, Reader, Mapper und Services (`Enset.Infrastructure`)
- Entwickler-Test-Harness für CSV-/Excel-Importe (`Enset.Worker.Import`)

Eine physische Data Lake Struktur mit Raw/Silver/Gold Zones ist derzeit nur konzeptionell beschrieben, aber nicht als separate Storage Layer im Code umgesetzt.

## Geplante Datenzonen

- Raw Zone: Originaldateien, Import-Rohdaten
- Silver Zone: validierte, normalisierte Daten
- Gold Zone: KPIs, Benchmark-Datasets und Data Products

## Implementierte Kernbausteine

- `MeterReading` als Zeitreihendaten mit `MeterId + Timestamp` als Schlüssel
- `Meter` mit fachlicher Identität `MeterNumber`
- `Customer`, `Project`, `Building`, `EnergySystem`, `Document`
- `CalculationResult` und `BenchmarkDataset` als vorbereitete Analyse-Entities

## Noch offen

- echtes Data Lake Storage-Layout
- automatisierte Raw/Silver/Gold-Pipeline
- exportfähige Data Products
- API und Frontend
- persistente ImportJob-/DataSource-Entitäten im DbContext
