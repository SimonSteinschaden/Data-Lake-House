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
| Raw Zone | teilweise | dateibasierte Archivierung nach ImportId und SHA-256 vorhanden; produktiver Storage, Retention und Zugriffsschutz fehlen |
| Curated/Silver | offen | Domain-Persistenz existiert, jedoch kein freigegebener Curated-Importpfad |
| Data Products/Gold | offen | keine Verträge, Publisher oder Produktpersistenz |
| PostgreSQL | teilweise | DbContext und Migrationen vorhanden, produktiver Importwriter fehlt |
| TimescaleDB | teilweise | kompatibles Modell vorhanden, Hypertable-Betrieb nicht nachgewiesen |

## Abgrenzung

Der `ImportReport` wird derzeit dateibasiert persistiert, ist aber weiterhin ein Workflow- und Auditmodell und kein Data Product. Ebenso sind `CalculationResult` und `BenchmarkDataset` vorbereitete Entities, aber noch keine versionierten Data Products.

## Verbleibende Arbeiten

- produktive Raw-Ablage, Retention, Zugriffsschutz und vollständige Provenance;
- transaktionaler Curated-/Database-Writer;
- datenbankgestützte Report- und Importhistorien-Persistenz;
- Background Jobs und Wiederanlauf;
- Berechnungs-, QA- und Publikationspipeline;
- Data-Product-Ports und standardisierte Ausgabe.
