# Data Lake House

## Zweck

Das ENSET Data Lake House soll strukturierte Energieberatungs-, Gebäude- und Zeitreihendaten zusammenführen. Die verbindliche Zieldefinition bleibt die Architecture Baseline v1.0.

## Aktuell implementiert

- Domainmodell für Customers, Projekte, Gebäude, Energiesysteme, Zähler und Messwerte;
- EF-Core-Context mit PostgreSQL/Npgsql-Unterstützung;
- TimescaleDB-kompatibles Zeitreihenmodell für `MeterReading`;
- Excel- und CSV-nahe Importadapter;
- Application-gesteuerte Excel-Importanalyse;
- vorbereitete Analyse-Entities `CalculationResult` und `BenchmarkDataset`.

## Zonenstatus

| Zone/Baustein | Status | Befund |
|---|---|---|
| Raw Zone | offen | `RawDataObject` existiert, aber kein Store oder Writer |
| Curated/Silver | offen | Domain-Persistenz existiert, jedoch kein freigegebener Curated-Importpfad |
| Data Products/Gold | offen | keine Verträge, Publisher oder Produktpersistenz |
| PostgreSQL | teilweise | DbContext und Migrationen vorhanden, produktiver Importwriter fehlt |
| TimescaleDB | teilweise | kompatibles Modell vorhanden, Hypertable-Betrieb nicht nachgewiesen |

## Abgrenzung

Der derzeitige `ImportReport` ist ein flüchtiges Analyseergebnis und kein persistiertes Data Product. Ebenso sind `CalculationResult` und `BenchmarkDataset` vorbereitete Entities, aber noch keine versionierten Data Products.

## Verbleibende Arbeiten

- unveränderliche Raw-Ablage und Provenance;
- transaktionaler Curated-/Database-Writer;
- Report- und Importhistorien-Persistenz;
- Background Jobs und Wiederanlauf;
- Berechnungs-, QA- und Publikationspipeline;
- Data-Product-Ports und standardisierte Ausgabe.
