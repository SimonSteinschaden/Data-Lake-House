# ADR: Association Management

## Kontext

ENSET benötigt eine einheitliche Bedienoberfläche für mehrere fachlich
unterschiedliche Beziehungen. Einige Beziehungen waren bereits Join-Entities,
andere nur direkte Foreign Keys. Eine universelle Tabelle mit
`EntityType/EntityId` würde Foreign Keys und fachliche Constraints verlieren.

## Entscheidung

Die UI und Application-Contracts sind generisch. Die Kompatibilitätsmatrix
wird zentral vom Backend ausgeliefert. Persistiert wird je Beziehung in
typisierten Assignment-Entities. Bestehende Direkt-FKs bleiben, wo Canonical
Snapshots sie verwenden, als synchronisierte aktuelle Projektion erhalten.

Beziehungen werden durch Preview, serverseitige Konfliktprüfung und einen
atomaren Command geändert. Historisierung erfolgt über `ValidFrom/ValidTo`;
normales Entfernen beendet die Beziehung. Primärwechsel sind atomar und
auditierbar.

## Alternativen

Eine universelle polymorphe Tabelle wurde wegen fehlender referenzieller
Integrität, schwacher Querybarkeit und verteilter Validierung verworfen.
Nur direkte Foreign Keys wurden verworfen, weil Rolle, Gültigkeit und Historie
nicht zuverlässig darstellbar wären. Eine zweite Snapshot-Persistenz wurde
ebenfalls verworfen.

## Konsequenzen

Neue Beziehungstypen benötigen Definition, typisierte Entity, Validierung und
Adapter. Das ist mehr Code, schützt aber Foreign Keys und fachliche Regeln.
Frontend-Erweiterungen sind dagegen datengetrieben. Aktuelle FKs und Historie
müssen transaktional synchron bleiben.

## Spätere Erweiterungen

PostgreSQL-Exclusion-Constraints für Zeiträume, gespeicherte Quality-Trends,
dedizierte Project-/Document-Snapshots, Verantwortlichenzuweisung und feinere
Autorisierung können additiv ergänzt werden.
