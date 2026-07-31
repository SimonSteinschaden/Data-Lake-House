# Objektanalyse

## Architektur

Die Objektanalyse ist ein interaktiver Consumer der Canonical-Snapshot-
Schicht:

`React → /api/v1/object-analytics → IObjectAnalyticsService →
ICanonicalSnapshotReader`.

Controller und Frontend enthalten keine EF- oder SQL-Auswertung.
`CanonicalObjectAnalyticsService` berechnet das versionierbare
`ObjectAnalyticsProduct`. Reports speichern genau dieses Produkt und
implementieren keine zweite Fachlogik.

## Suche und Filter

Die globale Suche berücksichtigt case-insensitive Teilstrings in Objektname,
Objektnummer, Kunde, Gemeinde, Adresse und Zählpunktnummer. Energieträger,
Zeitraum, Objekttyp, Quality Level und Kunde sind kombinierbar. Pagination
erfolgt nach der Snapshot-basierten Filterung.

## Kennzahlen und Lineage

| Kennzahl | Quelle/Data Product | Regel | Einheit | Fehlende Daten |
|---|---|---|---|---|
| Verbrauch | Meter Consumption Summary | Summe gültiger `IntervalValue`-Energiewerte; Wh/MWh werden nach kWh konvertiert | kWh | nicht verfügbar |
| Strom/Wärme | Meter Consumption Summary | Verbrauch zusätzlich nach `Meter.Medium` | kWh | 0 bei vorhandener Analyse ohne diesen Träger |
| Erzeugung | Annual Energy Balance | Summe der Energiewerte mit `Direction=Production` | kWh | nicht verfügbar |
| Spitzenlast | Peak Load Profile | Maximum konvertierbarer Power-Werte im Zeitraum | kW | nicht verfügbar |
| kWh/m² | Building Energy Profile | Verbrauch geteilt durch erste verfügbare kanonische Fläche: Heated, Conditioned, Net, Gross | kWh/m² | nicht verfügbar |
| kWh/Nutzer | – | keine kanonische Nutzerzahl vorhanden | kWh/Nutzer | nicht verfügbar |
| Kosten | – | kein freigegebenes Tarif-Data-Product | EUR | nicht verfügbar |
| CO₂ | – | kein freigegebenes Emissionsfaktor-Data-Product | kg CO₂e | nicht verfügbar |
| Eigenversorgungsgrad | Annual Energy Balance | Produktion / (Verbrauch + Produktion) | % | nicht verfügbar ohne Erzeugung |
| Eigenverbrauch | – | zeitgleiche Verbrauch-/Erzeugungszuordnung nicht vorhanden | % | nicht verfügbar |
| Vorjahr | Meter Consumption Summary | identischer Zeitraum minus ein Jahr; absolut und prozentual | kWh/% | nicht verfügbar ohne Vorjahreswert |

Jeder `AnalyticsValue` enthält Wert, Einheit, Verfügbarkeitsstatus,
Data-Product-Quelle, Berechnungsregel, Quality und Suitability.

## Diagramme

Monatsverbrauch wird nach Kalendermonat summiert. Tages- und Wochenprofile
aggregieren kanonische Leistungswerte nach Stunde beziehungsweise Wochentag
und Stunde. Die React-Balken zeigen per Hover Zeitpunkt und Wert.
Verbrauch nach Energieträger wird als Tabelle dargestellt.
Eine Auswertung nach Nutzungsart bleibt leer, solange Meter keine eindeutige
kanonische Nutzungsart besitzen.

## Regeln für Auffälligkeiten

Nur folgende dokumentierte Regeln werden angewendet:

- hoher Verbrauch: Objektwert über 150 % des Mittels von mindestens drei
  Gebäuden desselben kanonischen Typs;
- niedriger Verbrauch: unter 50 % desselben Vergleichswerts;
- Lastspitze: mindestens 24 Power-Werte und Maximum größer als dreifacher
  Median;
- fehlende Messwerte: keine konvertierbaren Intervallenergiewerte im
  Zeitraum;
- fehlender Jahreswert: `AnnualValueStatus` ist nicht `CompleteYear`;
- niedrige Qualität: Building-Snapshot ist Bronze;
- fehlende Zähler oder Energieträger: kanonische Zuordnung fehlt.

Es werden keine statistischen oder fachlichen Schwellen geraten.

## Vollständigkeit

Messwerte, Stammdaten, Jahreswerte und Anlagen werden als Grün, Gelb oder Rot
projiziert. Dokumente sind Gelb, weil der aktuelle Canonical Snapshot keine
Dokumentzusammenfassung enthält. Quality Level und Suitability bleiben
getrennte Dimensionen.

## Benchmark

Ein Benchmark wird nur mit mindestens drei weiteren Gebäuden desselben
kanonischen `BuildingType` und mit Verbrauch im selben Zeitraum gebildet.
Verwendet wird das arithmetische Mittel. Andernfalls lautet der Status
„Benchmark derzeit nicht verfügbar“.

## Performance

Pro Request wird ein Portfolio-Snapshot gelesen und anschließend
deterministisch im Speicher projiziert. Damit bestehen keine zusätzlichen
EF-Abfragen in Analytics. Die bekannte N+1-Einschränkung innerhalb des
aktuellen Portfolio-Snapshot-Readers bleibt bestehen und begrenzt sehr große
Portfolios. Serverseitige Snapshot-Pagination wäre eine spätere Erweiterung
des Reader-Contracts.
