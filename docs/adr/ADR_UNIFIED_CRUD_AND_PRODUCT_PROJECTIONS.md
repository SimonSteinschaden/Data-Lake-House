# ADR: Unified CRUD and Product Projections

- Status: Accepted
- Date: 2026-07-30

## Context

CRUD-Lesewege, Dashboard und Internal Data Products projizierten dieselben
Persistenzdaten mit teilweise abweichender Fachlogik.

## Decision

Dashboard, CRUD-Leseansichten und Internal Data Products verwenden die
bestehenden `ICanonicalSnapshotReader`-Contracts als gemeinsame fachliche
Quelle. Relationale EF-Entities bleiben Write Model sowie Quelle für
technische Metadaten, Berechtigungsprüfung und Rohmesswerte.

Customer- und EnergySystem-Snapshots werden nicht zusätzlich materialisiert.
Der bestehende synchrone On-demand-Reader garantiert nach einem erfolgreichen
CRUD-Commit aktuelle Folgeabfragen.

## Consequences

Jahreswert, Messwertzeitraum, Zuordnungen und Quality Level besitzen eine
zentrale Semantik. Die REST-Erweiterungen sind additiv. Es entsteht keine
Migration, keine neue Persistenz und kein Event Bus. Die Optimierung der
internen Portfolio-Abfragen bleibt separat möglich, ohne Contracts oder
fachliche Semantik zu ändern.
