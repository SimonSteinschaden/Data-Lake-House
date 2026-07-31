# ADR: Internal-Data-Product-Schicht

Status: Accepted, Ergänzung zur eingefrorenen
`ARCHITECTURE_BASELINE_V2_0.md`

## Entscheidung

Eine schreibgeschützte Application-Schicht stellt interne, unveränderliche
Summary-Produkte zwischen bestehenden ReadModels/Gold Profiles und interner
REST API bereit. Produktbezogene Interfaces vermeiden ein generisches
Framework. Die Implementierung projiziert mit EF Core und respektiert den
bestehenden DataAccessScope.

Das Dashboard konsumiert ausschließlich Portfolio- und Import-Quality-Summary.
CRUD-Endpunkte bleiben für Erfassen, Bearbeiten, Deaktivieren und
Wiederherstellen zuständig.

## Konsequenzen

Fachliche Aggregation liegt im Backend. Inkompatible Einheiten führen zu
gruppierten Ergebnissen und einer transparenten Warnung statt einer falschen
Gesamtsumme. Es gibt keine neue Persistenz, Migration, Cache-, Benchmark-,
Export- oder Zeitreihenkomponente. Diese ADR erweitert die Baseline, ändert
ihren Architecture Freeze jedoch nicht.

