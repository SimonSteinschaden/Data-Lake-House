# Datenqualität

## Qualitätsmodell

Datenqualität bewertet den Zustand kanonischer Customer-, Building- und
Meter-Snapshots. Quality Level (`Bronze`, `Silver`, `Gold`) bleibt strikt von
der anwendungsfallspezifischen Suitability für LEB, Navigator, Benchmark und
ISO 50001 getrennt.

## Kennzahlen und Dashboard

`GET /api/v1/data-quality/dashboard` liefert Vollständigkeit, Quality-Level-
Verteilung, Suitability, offene technische Import-Issues, offene
Datenprüfungen sowie Problemkennzahlen mit Anzahl, Schweregrad, Trend und
betroffenen Kunden, Objekten und Zählpunkten.

Details und Trends:

- `GET /api/v1/data-quality/problems/{code}`
- `GET /api/v1/data-quality/problems/{code}/trend?days=30`

Das Dashboard verlinkt für jedes Problem den passenden operativen Workflow.
Technische Importdaten werden nur für Import-Issues verwendet; fachliche
Werte stammen ausschließlich aus Canonical Snapshots.

## Erweiterbarkeit

Historische Trends werden derzeit als stabiler On-demand-Snapshot
bereitgestellt. Persistierte Zeitreihen für Qualitätskennzahlen,
Verantwortliche und ein eigener Ignore-Workflow können später ergänzt werden,
ohne eine zweite fachliche Snapshot-Persistenz einzuführen.
