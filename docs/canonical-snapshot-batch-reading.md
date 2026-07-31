# Canonical Snapshot Batch Reading

## Ausgangsproblem

Bis zu dieser Änderung implementierte `GetPortfolio` folgendes Muster:

```text
IDs laden
foreach CustomerId -> GetCustomer
foreach BuildingId -> GetBuilding
foreach MeterId -> GetMeter
```

Jeder Einzelaufruf lud neben der Entität erneut Snapshotversionen und – bei
Building und Meter – CuratedFieldValues. Buildings luden außerdem Versionen,
Adresse, Gemeinde, Regionen und Assignments; Meter luden ihren
Messwertbestand. Alle Aufrufe erfolgten seriell über denselben DbContext.

Die frühere Queryformel war:

```text
3 ID-Queries
+ 2 * CustomerCount
+ 3 * BuildingCount
+ 3 * MeterCount
+ 1 EnergySystem-Query
```

Ein repräsentatives Portfolio mit jeweils 50 Customers, Buildings und Meters
erzeugte damit **404 SQL-Statements**. Eine Customer-Liste mit 50 Customers
ohne zugeordnete Buildings oder Meters benötigte bereits 104 Statements.
Zugeordnete Entitäten erhöhten die Zahl linear. Im gemeldeten lokalen
PostgreSQL-Szenario führte dieser Pfad zu teilweise mehr als 20 Sekunden und
HTTP-499-Abbrüchen.

## Neue Batch-Architektur

`ICanonicalSnapshotReader` besitzt additive Methoden:

```csharp
GetCustomers(IReadOnlyCollection<Guid> ids, ...)
GetBuildings(IReadOnlyCollection<Guid> ids, ...)
GetMeters(IReadOnlyCollection<Guid> ids, ...)
```

Die bisherigen Single-Entity-Methoden delegieren mit einer einzelnen ID an
diese Batch-Primitiven. Dadurch besitzen Single- und Batch-Pfad exakt
dieselben Mapper und Fachregeln.

`GetPortfolio` lädt weiterhin ausschließlich über die Canonical-Snapshot-
Schicht, ruft aber jede Batch-Methode genau einmal auf. Es gibt keine
asynchronen Snapshot-Einzelaufrufe in Entity-Schleifen mehr.

Pro Entitätstyp werden gemeinsam geladen:

- alle scopeberechtigten Entities;
- bei Buildings nur aktive BuildingVersions und Assignments;
- Address, Municipality und Regions für die aktive Version;
- Meterzuordnung und kanonische Messwerte;
- bestätigte, aktuell gültige CuratedFieldValues für alle IDs;
- aktuelle GoldProfileVersions für alle IDs.

CuratedFieldValues werden in einer Abfrage je Entitytyp geladen und danach
nach `EntityId` und `FieldName` gruppiert. GoldProfileVersions werden
ebenfalls je Entitytyp gebündelt und nach `EntityId` aufgelöst. Alle
fachlichen Read-Queries verwenden `AsNoTracking`.

CRUD, PortfolioSummaryProduct und LEB bleiben unverändert Konsumenten von
`GetPortfolio`; damit profitieren alle Pfade von derselben Optimierung, ohne
direkte EF-Fachprojektionen einzuführen.

## Query-Budget

Das Budget für einen vollständigen Portfolioaufbau beträgt **maximal zwölf
SQL-Statements**, unabhängig von der Entityanzahl:

| Gruppe | Statements |
|---|---:|
| scopeberechtigte Customer-, Building- und Meter-IDs | 3 |
| Customers und deren aktuelle Versionen | 2 |
| Buildings inklusive gefiltertem Graph, Kuration und Versionen | 3 |
| Meters inklusive Messwerten, Kuration und Versionen | 3 |
| EnergySystems | 1 |
| **Gesamt** | **12** |

Damit liegen Customer-, Building- und Meter-Listen im vorgegebenen Budget von
8 bis 12 Statements. Der Architekturtest schützt sowohl das Budget als auch
das Verbot von Single-Snapshot-Aufrufen in der Portfolio-Schleife.

Das Budget beschreibt die Npgsql-Übersetzung des implementierten Querypfads.
Die InMemory-Tests prüfen 50er-Batches und Semantik, können aber keine echten
SQL-Commands zählen. Ein Command-Interceptor-Test gegen PostgreSQL gehört zur
nachfolgenden produktionsnahen Integrationssuite.

## Single-vs-Batch-Semantik

Fachregeln wurden nicht verändert. Identisch bleiben:

- Customer-, Building- und Meter-Nummer sowie Name;
- Gemeinde und Zuordnungen;
- Quality Level und Suitability;
- AnnualValue, Status, Einheit und Referenzjahr;
- MeasurementCount, PeriodStart und PeriodEnd;
- originale MeterNumber und getrenntes Meter.Name.

Ein Regressionstest vergleicht die gemeinsamen Meter-, Quality-,
Suitability- und ReadingSummary-Werte zwischen Single- und Batch-Aufruf.

## Entwicklungsbenchmark

| Messung | Vorher | Nachher |
|---|---:|---:|
| SQL-Anzahl, 50/50/50-Portfolio | 404 | 12 |
| Wachstum der SQL-Anzahl | linear je Entity | konstant |
| gemeldete lokale Laufzeit | teilweise >20 s | in dieser Umgebung nicht mit lokaler PostgreSQL-Instanz reproduzierbar |

Das Ziel „deutlich unter zwei Sekunden“ bleibt ein Entwicklungsbenchmark und
kein plattformunabhängiger Test. Für eine belastbare Zeitangabe müssen
derselbe Datenbestand, PostgreSQL, Netzwerk, Warm-up und Querylogging
verwendet werden. Es wird bewusst keine InMemory-Zeit als
PostgreSQL-Laufzeit ausgegeben.

## Bekannte Einschränkungen

- `GetPortfolio` lädt weiterhin den vollständigen berechtigten fachlichen
  Datenbestand; die SQL-Anzahl ist konstant, Speicher- und Datenvolumen sind
  es nicht.
- Die Meterprojektion benötigt für LEB weiterhin kanonische Einzelmesswerte.
- Große Reading-Bestände können daher Materialisierungszeit und Speicher
  dominieren, obwohl kein N+1 mehr besteht.
- `includeDeleted`, NU1903 und allgemeine TimescaleDB-Optimierung sind nicht
  Teil dieser Änderung.

## Spätere Materialisierungsoptionen

Bei weiter wachsendem Datenbestand können auf Basis derselben Contracts
gebündelte paginierte Canonical List Projections oder die kontrollierte
Materialisierung in der bestehenden GoldProfileVersion-/Snapshotarchitektur
ergänzt werden. Eine zweite Snapshot-Persistenz oder direkte EF-Fachlogik in
CRUD, Products oder Export ist dafür nicht erforderlich.
