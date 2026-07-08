# KPIs und Benchmarking

## Implementierter Modellstand

Im Domain-Layer sind zwei persistierbare Ergebnisstrukturen vorhanden:

### CalculationResult

- `KPIType`: Consumption, CO2, Savings, Autarky oder SelfConsumption
- `ScopeLevel`: Region, Municipality, District oder Building
- `ScopeId`
- `Value` und `Unit`
- `PeriodStart`, `PeriodEnd` und `CalculatedAt`

### BenchmarkDataset

- `ScopeLevel`
- `Region`
- `BuildingCategory`
- `YearRange`
- `AvgConsumption`
- `SampleSize`

Beide Entities sind als DbSets im `EnsetDbContext` registriert.

## Noch nicht implementiert

- Calculation- und Benchmark-Services mit fachlicher Rechenlogik;
- automatische Aggregation über Gebäude, District, Municipality und Region;
- Datenqualitäts- und Berechnungsmethoden im Ergebnismodell;
- Anonymisierung und Mindestgruppengrößen;
- QA-, Versionierungs- und Publikationsschritte für Data Products;
- API und UI für KPI-Abfragen.

Die vorhandenen Entities sind vorbereitete Analyseergebnisse. Sie dürfen nicht mit vollständig publizierten Data Products gleichgesetzt werden.

## Datenschutzanforderung

Aggregierte oder extern bereitgestellte Ergebnisse dürfen keine Rückschlüsse auf einzelne Customers, Adressen, Gebäude oder Zählpunkte ermöglichen. Die konkrete Anonymisierungs- und Freigabelogik ist noch offen und bleibt Teil der Baseline-Umsetzung.
