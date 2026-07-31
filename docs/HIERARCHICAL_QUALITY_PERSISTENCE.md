# Persistenz des hierarchischen Qualitätsmodells – Phase 1

## Abgrenzung

Persistiert werden ausschließlich fachliche Fakten und Historie. Bronze,
Silver, Gold und Fortschrittswerte werden weiterhin ausschließlich durch
die zentrale Qualitätslogik berechnet. Canonical Snapshots, Data Products
und UI sind Gegenstand von Phase 2.

## Tabellen

- `BuildingInventoryDeclarations`: versionierte Inventarbestätigungen.
- `MeterProfileAnalyses`: unveränderliche technische Analyseversionen.
- `MeterProfileIssues`: historisch erhaltene Einzelbefunde.
- `MeterProfileCurationDecisions`: begründete, supersedierbare Entscheidungen.

Alle Beziehungen zu Gebäude, Zählpunkt, Analyse und Issue verwenden
`DeleteBehavior.Restrict`. `xmin` dient als Concurrency Token.

## Versionierung und Current-Semantik

Pro Gebäude beziehungsweise Zählpunkt erzwingen partielle PostgreSQL-
Unique-Indizes höchstens eine Zeile mit `IsCurrent = true`.
`BuildingId/MeterId + VersionNumber` ist zusätzlich eindeutig.

Eine neue Inventarerklärung invalidiert die vorherige aktuelle Erklärung.
Eine erfolgreich abgeschlossene Analyse supersediert die bisherige aktuelle
Analyse. Fehlgeschlagene und abgebrochene Analysen werden niemals aktuell
und verdrängen deshalb keine bestätigte Analyse.

## Issues und Entscheidungen

Issues werden nicht gelöscht oder nachträglich einer anderen Analyse
zugeordnet. Statusänderungen erfolgen durch Kurationsentscheidungen.
Entscheidungen benötigen Benutzer, Zeitpunkt und Begründung. Ersatzwerte
benötigen zusätzlich Methode und Confidence. Eine neue Entscheidung kann
mit `SupersedesDecisionId` auf ihre Vorgängerin verweisen.

## Audit und Berechtigungen

Der Service schreibt fachliche Operationen in die bestehende
`EntityAuditEntries`-Infrastruktur. Bestätigungen, Invalidierungen,
Analyseübergänge, Issues und Entscheidungen werden zusammen mit der
Fachoperation gespeichert.

Schreiboperationen verlangen serverseitig einen authentifizierten
ENSET-Mitarbeiter beziehungsweise ENSET-Administrator. Kundenrollen können
keine Inventarbestätigung oder finale Entscheidung erzeugen.

## Migration und Bestandsdaten

`AddHierarchicalQualityPersistence` legt ausschließlich die vier neuen
Tabellen, Constraints und Indizes an. Es gibt keinen Backfill: vorhandene
Zählpunkte bleiben ohne Analyse, Inventare unbestätigt und bestehende
Gold-Profil-Versionen unverändert.

## Bekannte Einschränkungen

Die produktive Anbindung und Invalidierung ist in
`HIERARCHICAL_QUALITY_INTEGRATION.md` dokumentiert.
