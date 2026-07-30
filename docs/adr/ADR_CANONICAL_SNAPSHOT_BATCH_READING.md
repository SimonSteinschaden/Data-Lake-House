# ADR: Canonical Snapshot Batch Reading

- Status: Accepted
- Date: 2026-07-30

## Context

`GetPortfolio` rief den Canonical Snapshot Reader seriell je Customer,
Building und Meter auf. Entity, Kuration und Snapshotversion verursachten
dadurch eine linear wachsende Zahl von Datenbank-Roundtrips.

## Decision

`ICanonicalSnapshotReader` erhält additive Batch-Methoden für Customers,
Buildings und Meters. `GetPortfolio` verwendet ausschließlich diese
Batch-Primitiven. Single-Entity-Methoden delegieren mit einer ID an denselben
Pfad.

CuratedFieldValues und GoldProfileVersions werden je Entitytyp gemeinsam
geladen und gruppiert. Fachliche Verbraucher bleiben ausschließlich von der
Canonical-Snapshot-Schicht abhängig.

## Consequences

- Das vollständige Portfolio besitzt ein konstantes Budget von zwölf
  Statements statt einer linearen Queryanzahl.
- Single- und Batch-Semantik können nicht auseinanderlaufen.
- Es entsteht keine Snapshot-Persistenz, Migration oder Messaging-
  Infrastruktur.
- Der vollständige Portfolioinhalt wird weiterhin materialisiert; eine
  spätere paginierte Canonical List Projection bleibt möglich.
