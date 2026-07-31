# ADR: LEB Export from Canonical Data

- Status: Accepted
- Date: 2026-07-30

## Context

Der bisherige LEB-Builder verwendete fachliche EF-Abfragen für
Gebäudeversionen, Kunden, Meter, Messwerte und Energieanlagen.

## Decision

Alle fachlichen Exportwerte stammen ausschließlich aus
`ICanonicalSnapshotReader`. `LebExportDataset` ist die gemeinsame Grundlage
für Validate, CSV und Excel. Die Serializer konsumieren weiterhin nur
`NoeLebExportContractV1`.

Quality Level Bronze/Silver/Gold ist keine Exportfreigabe.
Anwendungsfallspezifische LEB Suitability entscheidet über die Eignung.

## Consequences

Direkte EF- und Curated-Field-Zugriffe entfallen aus dem Export. Contract V1
und Dateiformate bleiben unverändert. Es entstehen keine Snapshot-Persistenz,
Migration oder Messaging-Infrastruktur.
