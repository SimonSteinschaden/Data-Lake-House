# Dashboard 2.0

## Ziel und Struktur

Das Dashboard ist ein internes Energie- und Datenqualitätscockpit für
Überblick, Bewertung, Priorisierung und Navigation. Die Seite enthält die
Bereiche Portfolio, Energie, Datenqualität, Data-Product-Readiness sowie
Import- und Bearbeitungsstatus.

## Datenquellen

Die Seite führt beim Laden genau zwei Requests aus:

- `GET /api/v1/internal-data-products/portfolio/summary`
- `GET /api/v1/internal-data-products/import-quality`

Sie ruft keine CRUD-, Curation-, Import- oder Gold-Endpunkte auf. Fachliche
Aggregation bleibt im Backend.

## UI-Komponenten und Kennzahlen

`DashboardSection`, `DashboardKpiCard`, `EnergySummaryChart`,
`EnergySummaryTable`, `DataQualityActionList`, `ReadinessCard`,
`BlockerList`, `ImportQualityOverview`, `LoadingDashboardSkeleton`,
`EmptyDashboardState` und `ErrorDashboardState` bilden die Cockpitansicht.

Portfolio zeigt Bestands- und Aktivzahlen, freigegebene Gold-Profile und
Gold-Reife. Datenqualität zeigt Gold-Lücken, fehlende Zählpunkte und Messwerte,
unvollständige Profile, Kurationsaufgaben sowie Importprobleme.

Das Balkendiagramm erzeugt pro Einheit eine eigene Gruppe. Die Länge eines
Balkens ist nur eine visuelle Skalierung innerhalb derselben Einheit. Die
Tabelle bleibt die verbindliche Darstellung der Werte, Einheiten, Richtungen
und Zählpunktanzahlen. Es findet keine Einheitenumrechnung statt.

Readiness-Karten zeigen Voraussetzungen, Prozentsatz, vorbereitete und
blockierte Scopes, zentrale Blocker und Empfehlungen. Sie stellen keine
Benchmark- oder Profilergebnisse dar.

## Navigation

KPI- und Aktionslinks führen nach `/customers`, `/buildings`, `/meters`,
`/tools/data-curation` oder `/imports`. Readiness-Karten verlinken auf die
betroffenen Objekt- beziehungsweise Zählpunktlisten.

## Einschränkungen

Es werden keine Zeitreihen, Hochrechnungen, Benchmarks, normalisierten Profile,
Kosten- oder CO₂-Werte berechnet. Ein unbekannter Jahreszeitraum bleibt
unbekannt. Nicht verfügbare Importattribute werden als solche angezeigt.
EEG/P2P werden mangels belastbarer Portfolio-Voraussetzungen nicht dargestellt.
