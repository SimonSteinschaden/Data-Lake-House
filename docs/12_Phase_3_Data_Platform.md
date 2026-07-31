# Phase 3 – Operative Datenplattform

**Status:** verbindlicher technischer Blueprint für Phase 3  
**Stand:** 19.07.2026  
**Grundlage:** `Decisions/ARCHITECTURE_REVIEW_V1_2.md` und tatsächlicher Repository-Stand

## 1. Ziel und Abgrenzung

Phase 3 baut die operative Datenplattform des ENSET Data Lake House auf. Der Import bleibt ein Zuführungsweg und wird nur so weit angepasst, wie für transaktionale, nachvollziehbare Domain-Persistenz erforderlich. Die bestehenden Grenzen `Enset.Domain`, `Enset.Application`, `Enset.Infrastructure`, `Enset.Api`, `Enset.Worker`, `Enset.Web` und Tests bleiben erhalten.

Die Plattform soll Monitoring, Energiecontrolling, Benchmarking, Reporting, Energieausweise, Sanierungsplanung und kommunales Energiemanagement tragen. Data Products, Business Modules, vollständige Authentifizierung/Mandantenfähigkeit, Kubernetes und Messaging sind nicht Bestandteil dieser Phase.

### Phase-3-Prinzipien

- PostgreSQL ist System of Record für Stammdaten, Beziehungen, Historie, Definitionen und berechnete Ergebnisse.
- TimescaleDB speichert und aggregiert Messzeitreihen auf Basis desselben PostgreSQL-Clusters.
- Object Storage hält Binärdokumente und Raw-Dateien; PostgreSQL speichert nur Metadaten und stabile Object Keys.
- Application Use Cases bilden die einzige Geschäftsgrenze für API, Worker und künftige Hosts.
- Importpfade verwenden dieselben Domain-Persistenzports wie andere Ingestion-Kanäle, umgehen aber niemals WriteGate und Commit.
- Daten werden fachlich deaktiviert oder historisiert, nicht physisch gelöscht, solange Aufbewahrungsregeln nichts anderes verlangen.
- regulatorische Anforderungen werden über versionierte Definitionen, Attribute und Reports erweitert, nicht über ständig neue Spalten im Kernmodell.

## 2. Zielarchitektur

```text
Enset.Web
   |
   | generated NSwag client
   v
Enset.Api ---------------------> Application query/command use cases
                                      |
Enset.Worker --------------------------+
                                      |
                                      v
                              Domain model + policies
                                      |
                                      v
                         Infrastructure adapters
                          /        |         \
                    PostgreSQL  TimescaleDB  Object Storage
```

API und Worker bleiben getrennt deploybar. `Enset.Api` stellt synchrone Queries und Commands bereit. `Enset.Worker` übernimmt in Phase 3 geplante Aggregations-, Qualitäts- und Reportingläufe, sobald diese nicht mehr sinnvoll im Requestpfad laufen. Beide verwenden dieselben Application Services; kein Controller und kein Worker greift direkt auf `EnsetDbContext` zu.

### Vorgeschlagene fachliche Application-Features

```text
Enset.Application/
├── Customers/
│   ├── GetCustomer
│   └── UpdateCustomer
├── Buildings/
│   ├── ListBuildings
│   ├── GetBuildingDetails
│   └── UpdateBuildingProfile
├── EnergySystems/
│   └── GetBuildingEnergySystems
├── Meters/
│   ├── ListBuildingMeters
│   └── GetMeter
├── MeterReadings/
│   ├── IngestMeterReadings
│   ├── QueryMeterReadings
│   └── AggregateMeterReadings
├── Analytics/
│   ├── CalculateKpis
│   ├── QueryKpis
│   └── DetectDataQuality
├── Reporting/
│   ├── BuildEnergyReport
│   └── ExportReport
└── Imports/                       # bestehender Zuführungsprozess bleibt getrennt
```

Ein generischer CRUD-Service pro Entity wird nicht empfohlen. Jeder Service entspricht einem Use Case und schützt Validierung, Historisierung, Berechtigungs-Hooks und Transaktionsgrenzen.

## 3. Relationales PostgreSQL-Modell

### 3.1 Aggregate und Beziehungen

Das fachliche Zielbild ist:

```text
Customer
  -> CustomerBuildingAssignment
      -> Building
          -> BuildingVersion
          -> EnergySystem -> EnergySystemVersion
          -> Meter -> MeterVersion -> MeterReading
          -> Document
          -> EnergyCertificate
          -> RenovationMeasure
```

`Project` bleibt als fachlicher Arbeits-/Beratungskontext bestehen, darf aber nicht länger die einzige Eigentumsbrücke zwischen Customer und Building sein. Der aktuelle Code `Customer -> Project -> Building` verhindert Portfoliozuordnung ohne Projekt und erschwert Eigentümerwechsel. Deshalb wird eine direkte zeitlich gültige Zuordnung `customer_building_assignments` ergänzt. `ProjectBuildingAssignment` kann Projekte weiterhin mit Gebäuden verbinden.

### 3.2 Kerntabellen

| Tabelle | Kernfelder | Wichtige Regeln |
|---|---|---|
| `customers` | `id`, `name`, `type`, `is_active`, Zeitstempel | kein Hard Delete; fachliche Fremd-IDs separat |
| `buildings` | `id`, stabiler Schlüssel, Adresse/Geografie, `is_active` | nur zeitinvariante Identität; veränderliche Eigenschaften in Versionstabelle |
| `building_versions` | `building_id`, `version`, `valid_from`, `valid_to`, Fläche, Nutzung, Kategorie, Eigentumsart, Baujahr | `(building_id, valid_from)` unique; keine überlappenden Gültigkeiten |
| `customer_building_assignments` | Customer, Building, Beziehungstyp, Anteil, `valid_from/to` | historisierte Portfolio-/Eigentumsbeziehung |
| `projects` | Customer, Name, Start/Ende, Status | bestehendes Modell weiterverwenden und optionales Ende ergänzen |
| `project_buildings` | Project, Building, `valid_from/to` | löst Building vom zwingenden `ProjectId` |
| `energy_systems` | ID, Building, Systemart, `is_active` | stabile Anlagenidentität |
| `energy_system_versions` | Gültigkeit, Energieträger, Leistung, Inbetriebnahme, Wirkungsgrad, erneuerbarer Anteil | keine überlappenden Versionen |
| `meters` | ID, Building optional, MeterNumber, Messgröße, `is_active` | normalisierte MeterNumber je Quellsystem unique |
| `meter_versions` | Gültigkeit, Einheit, Medium/Energieträger, Faktor, Zählertyp, Ein-/Ausbau | Zählerwechsel nachvollziehbar |
| `documents` | ID, optional Building/Project/Customer, Typ, ObjectKey, Hash, MediaType, Größe, Status | Binärinhalt nicht in DB; unveränderliche Versionen |
| `external_identifiers` | EntityType, EntityId, SourceSystem, ExternalId, `valid_from/to` | Unique Constraint auf aktive Quelle/ID |
| `energy_certificates` | Building, Typ, Ausstellungs-/Ablaufdatum, Methodikversion, Kennwerte, DocumentId | versioniert und nicht als Building-Spaltenblock |
| `renovation_measures` | Building, Maßnahme, Status, Zeitraum, Kosten-/Energie-/CO2-Schätzung, Abhängigkeiten | Grundlage für Sanierungsfahrplan, nicht vollständige EPBD-Fachanwendung |

### 3.3 Anpassung des vorhandenen Domainmodells

| Vorhandene Klasse | Bewertung | Phase-3-Maßnahme |
|---|---|---|
| `Customer` | wiederverwendbar | Aktivstatus und direkte Building-Zuordnungen ergänzen; Projects behalten |
| `Project` | wiederverwendbar | als Arbeitskontext behalten; Building-Beziehung in Zuordnungstabelle überführen |
| `Building` | anzupassen | stabile Identität von versionierten Eigenschaften trennen; District-Bezug behalten |
| `EnergySystem` | anzupassen | stabile Anlage plus Versionen; Energieträger und technische Kennwerte ergänzen |
| `Meter` | anzupassen | Versions-/Einbauhistorie; Messgröße und Quellsystem explizit modellieren |
| `MeterReading` | wiederverwendbar mit Anpassungen | UTC/Offset-Regel, Source/Quality/Revision ergänzen; redundante Customer-/BuildingIds entfernen oder nur abgeleitet halten |
| `Document` | anzupassen | nicht nur Project; ObjectKey/Hash/Version/Status statt lokalem `FilePath` |
| `CalculationResult` | anzupassen | `KPIType`-Enum nicht als Erweiterungsmechanismus verwenden; Definition/Version referenzieren |
| `BenchmarkDataset` | anzupassen | Dataset-ID/Version/Quelle/Gültigkeit und nachvollziehbare Segmentdefinition ergänzen |

## 4. TimescaleDB-Modell

### 4.1 Messwerttabelle

```sql
meter_readings
---------------
meter_id             uuid        not null
timestamp_utc        timestamptz not null
value                 numeric     not null
unit_code             text        not null
quality_code          text        not null
source_id             uuid        null
ingested_at           timestamptz not null
revision              integer     not null default 1
is_correction         boolean     not null default false
```

Der logische Schlüssel bleibt `meter_id + timestamp_utc`; Korrekturen dürfen entweder als neue Revision historisiert oder in einer separaten `meter_reading_revisions`-Tabelle gespeichert werden. Für das MVP wird eine append-only Revisionstabelle empfohlen, während `meter_readings_current` als View die jeweils gültige Revision liefert. Damit gehen Originalwerte bei Korrekturen nicht verloren.

Die bestehende Migration namens `ConfigureMeterReadingTimescale` erzeugt nach aktuellem Review keinen nachgewiesenen Hypertable. Phase 3 muss `CREATE EXTENSION IF NOT EXISTS timescaledb` als Betriebsentscheidung und `create_hypertable` in einer idempotenten, getesteten Migration bzw. Deploymentmigration ausführen.

### 4.2 Partitionierung, Indizes und Retention

- Hypertable-Zeitdimension: `timestamp_utc`; Space-Partitionierung nach `meter_id` erst nach Lasttest.
- Index: `(meter_id, timestamp_utc desc)`.
- Keine automatische Rohdatenlöschung im MVP.
- Compression/Columnstore-Policy erst nach Messung; ältere Chunks können komprimiert werden.
- Retention ist eine fachliche und regulatorische Entscheidung und darf nicht implizit aktiviert werden.
- UTC wird gespeichert; lokale Zeitzone gehört zum Building/Meter und wird nur für Kalenderaggregation verwendet.
- Sommerzeitgrenzen müssen in Tages-/Wochenaggregation explizit getestet werden.

### 4.3 Continuous Aggregates

| View | Bucket | Kennwerte |
|---|---|---|
| `meter_readings_daily` | lokaler/definierter Tag | Summe, Min, Max, Mittel, Anzahl, erwartete Anzahl, Coverage |
| `meter_readings_weekly` | ISO-Woche | Summe, Peak, Coverage |
| `meter_readings_monthly` | Kalendermonat | Summe, Peak, Verbrauch je Fläche optional über KPI-Layer |
| `meter_readings_yearly` | Kalenderjahr | Summe, Vergleich Vorjahr, Coverage |

Flächen- oder CO2-Kennzahlen gehören nicht direkt in Continuous Aggregates, weil Flächen, Faktoren und Energieträger historisiert sind. Sie werden im KPI-Layer mit „as-of“-Versionen berechnet.

## 5. Transaktions- und Persistenzgrenzen

Der erste operative Vertical Slice umfasst Customer, Building, Meter und MeterReading. `DatabaseImportWriter` bleibt ein Infrastructure-Adapter und konsumiert nur den freigegebenen Snapshot aus dem bestehenden Commitpfad.

```text
ImportCommitService
  -> ImportWriteGate
  -> DatabaseImportWriter
      -> BeginTransaction
      -> upsert Customer + ExternalIdentifier
      -> upsert Building + Version + Assignment
      -> upsert Meter + Version
      -> insert MeterReadings/revisions in batches
      -> write CommitAttempt/Receipt/Audit
      -> Commit
```

Ein einzelner kleiner Import darf in einer Transaktion laufen. Große Zeitreihen werden in idempotenten Batches geschrieben; deren Batch-Receipts gehören zum CommitAttempt. Ein Retry mit derselben Import-/Snapshot-/Batch-ID darf keine Duplikate erzeugen. `Replace` bleibt blockiert, bis Lösch-/Historisierungssemantik fachlich definiert ist.

Application Services verwenden Ports, beispielsweise `ICustomerRepository`, `IBuildingRepository`, `IMeterRepository` und `IMeterReadingStore`, nur wenn diese unterschiedliche Aggregate/Storage-Semantik schützen. Ein generisches Repository wird nicht eingeführt. Für gemeinsame atomare Writes koordiniert eine Application Unit-of-Work-Grenze den Infrastructure-DbContext; `SaveChangesAsync` wird nicht in Mappern aufgerufen.

## 6. Historisierungskonzept

### Entscheidung: stabile Identität plus fachliche Versionstabellen

Für Building, EnergySystem und Meter wird eine bitemporale Vollimplementierung im MVP vermieden. Verwendet wird zunächst **fachliche Gültigkeit**:

- Basistabelle hält stabile Identität und Lifecycle (`is_active`, `created_at`).
- Versionstabelle hält veränderliche fachliche Attribute mit `valid_from`, `valid_to`, `recorded_at`, `recorded_by`, `change_reason` und `version`.
- `valid_to` ist exklusiv; die aktive Version hat `NULL`.
- Updates schließen die alte Version und fügen eine neue ein, innerhalb derselben Transaktion.
- Exclusion Constraints verhindern überlappende Gültigkeitszeiträume pro Aggregate.
- Korrekturen mit rückwirkender fachlicher Gültigkeit bleiben möglich und werden zusätzlich über `recorded_at` nachvollziehbar.

`audit_entries` dokumentiert, wer welchen Command ausgeführt hat, ist aber kein Ersatz für fachliche Versionstabellen. EF-interne Temporal Tables stehen in PostgreSQL nicht als SQL-Server-Feature zur Verfügung und werden nicht emuliert. Event Sourcing wäre für den aktuellen Bedarf überdimensioniert.

Hard Delete wird für fachliche Daten im Application Layer nicht angeboten. Datenschutzrechtlich erforderliche Löschung/Anonymisierung ist ein gesonderter administrativer Prozess für Phase 4; Audit darf dabei keine unnötigen personenbezogenen Payloads enthalten.

## 7. Flexible Kennzahlen-Engine

Das vorhandene `KPIType`-Enum ist für wenige feste Kennzahlen geeignet, aber nicht für EED-/EPBD-/OIB- und spätere Modul-Erweiterungen. Phase 3 führt ein definitionsbasiertes Modell ein:

| Tabelle/Typ | Verantwortung |
|---|---|
| `kpi_definitions` | stabiler Code, Name, Beschreibung, Ergebnisdimension, Standardaggregation, Gültigkeit |
| `kpi_definition_versions` | Formel-/Calculator-Key, Parameter-Schema, Methodik-/Rechtsstand, Version |
| `emission_factor_sets` | versionierte Faktoren nach Energieträger, Region und Zeitraum |
| `kpi_calculation_runs` | Scope, Zeitraum, Input-Watermark, Calculator-Version, Status |
| `kpi_results` | DefinitionVersion, ScopeType/ScopeId, Zeitraum, Wert, Einheit, Quality/Coverage, RunId |

Formeln werden nicht als frei ausführbarer C#- oder SQL-Text aus der Datenbank geladen. Eine Definition referenziert einen registrierten Calculator-Key, zum Beispiel `energy.final.total`, `energy.intensity.floor_area`, `power.peak`, `co2.operational`. Neue einfache Kennzahlen entstehen durch neue Definition/Parameter; neue Berechnungslogik durch eine neue `IKpiCalculator`-Implementierung und Registrierung. Das wahrt Testbarkeit und verhindert Code-Injection.

Jedes Ergebnis enthält Definition-/Calculator-Version, Zeitraum, Input-Watermark, Einheit, Datenabdeckung und Qualitätsstatus. Neuberechnungen erzeugen neue Runs; alte Ergebnisse bleiben reproduzierbar.

## 8. Gebäude- und Energiekennzahlen

P0-Kennzahlen:

- Endenergieverbrauch je Energieträger und Zeitraum
- Energieintensität `kWh/m²a` unter Verwendung der im Zeitraum gültigen Fläche
- maximale Leistung/Lastspitze und Zeitpunkt
- Datenabdeckung und fehlende Intervalle
- operatives CO2e anhand eines versionierten Emissionsfaktorsatzes
- Bedarf als getrennte berechnete/aus Zertifikat übernommene Größe; niemals mit gemessenem Verbrauch vermischen

Nutzung, Baujahr, Fläche und Energieträger sind Dimensionen bzw. historisierte Stammdaten, keine bloßen KPI-Ergebnisse. Jede API-Antwort kennzeichnet Quelle, Methodik und Zeitraum.

## 9. Vorbereitung EED III und EPBD

Diese Architektur ist eine technische Vorbereitung und keine Festlegung nationaler Rechtskonformität. Nationale Umsetzungen und OIB-Regeln werden später als versionierte Methodik-/Validation-Sets ergänzt.

Die Energieeffizienzrichtlinie (EU) 2023/1791 verlangt unter anderem eine belastbare Betrachtung des öffentlichen Sektors und nennt für Inventare öffentlicher Gebäude Fläche, gemessenen jährlichen Verbrauch von Wärme, Kühlung, Elektrizität und Warmwasser sowie Energieausweise. Das Modell muss deshalb öffentliche Eigentümerschaft, Flächenhistorie, Energieträger, Jahresaggregation und Zertifikate abbilden. Quelle: [Richtlinie (EU) 2023/1791](https://eur-lex.europa.eu/eli/dir/2023/1791/oj?locale=en).

Die neu gefasste EPBD (EU) 2024/1275 umfasst digitale Energieausweise, Gebäudedatenbanken und Renovierungspässe. Renovierungspässe enthalten unter anderem aktuellen Energiezustand, schrittweise Maßnahmen, Sequenz, geschätzte Einsparungen und breitere Wirkungen. Deshalb werden `energy_certificates`, `renovation_measures`, Methodikversionen und Dokumentreferenzen als spätere fachliche Aggregate vorbereitet. Quelle: [Richtlinie (EU) 2024/1275](https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX%3A32024L1275), [EU-Kommissionsübersicht zur EPBD](https://energy.ec.europa.eu/topics/energy-efficiency/energy-performance-buildings/energy-performance-buildings-directive_en).

Für kommunale Daten werden zusätzlich vorbereitet:

- Organization/PublicBody-Klassifikation am Customer
- hierarchische Portfolio-/Gebäudezuordnung
- sektorale Aggregation ohne personenbezogene Details
- Berichtsperioden und festgeschriebene Snapshots
- Datenqualitäts-/Coverage-Kennzahlen
- Export-Metadaten und Methodikversion

Konkrete Meldeformate werden nicht fest in Core-Entities eingebaut, sondern als Reporting-Projektionen implementiert.

## 10. REST-API-Konzept

Die bestehende `/api/v1`-Versionierung und NSwag-Generierung werden wiederverwendet. Neue Controller bleiben dünn und rufen Application Queries auf.

| Methode | Route | Zweck |
|---|---|---|
| `GET` | `/api/v1/buildings` | paginierte Gebäudeliste mit Filter auf Customer, Nutzung, Region, Aktivstatus |
| `GET` | `/api/v1/buildings/{buildingId}` | Detail einschließlich aktueller Version, Systeme und Summary-KPIs |
| `GET` | `/api/v1/buildings/{buildingId}/meters` | Zählerliste und Datenabdeckung |
| `GET` | `/api/v1/meters/{meterId}` | Zählerdetail und gültige Konfiguration |
| `GET` | `/api/v1/meters/{meterId}/readings` | begrenzte Rohmessreihe; `from`, `to`, Cursor |
| `GET` | `/api/v1/meters/{meterId}/series` | aggregierte Reihe; `bucket=day|week|month|year` |
| `GET` | `/api/v1/buildings/{buildingId}/kpis` | KPI-Ergebnisse nach Zeitraum/Definition |
| `GET` | `/api/v1/buildings/{buildingId}/data-quality` | Lücken, Coverage und Plausibilitätsbefunde |
| `GET` | `/api/v1/reports/buildings/{buildingId}` | Reportstatus/-metadaten; binärer Export separat |

Regeln:

- maximaler Zeitraum und maximale Rohpunkte pro Request; große Exporte über Reportjob, nicht über unbegrenzte JSON-Antworten;
- Cursor-Pagination für Messwerte, Offset/Cursor für Gebäudelisten;
- UTC-Zeitpunkte im Vertrag, explizite Anzeigezeitzone;
- ProblemDetails mit stabilen Type-URIs;
- ETag/Version für mutierende Stammdatenendpunkte;
- `CancellationToken` durch alle Schichten;
- keine EF-Entities im API-Vertrag;
- NSwag-Client bleibt generiert und wird im Web über Feature-Services gekapselt.

## 11. Reporting-Architektur

Reporting besteht aus drei Stufen:

1. Query-/KPI-Projektionen liefern normalisierte, versionierte Daten.
2. `ReportDefinition` wählt Abschnitte, KPIs, Zeitraum, Sprache und Template-Version.
3. Renderer erzeugen HTML/PDF, Excel oder CSV als Infrastructure-Adapter und speichern das Artefakt im Object Storage.

CSV eignet sich für tabellarische Zeitreihen, Excel für analysierbare Arbeitsmappen und PDF für festgeschriebene Berichte. Ein Report speichert Input-Watermark, KPI-RunIds, Template-Version, Hash und ObjectKey. Dadurch bleibt er auch nach Stammdatenänderungen nachvollziehbar. PDF-Erzeugung wird erst in P1 implementiert; in P0 werden Verträge und Metadaten vorbereitet.

## 12. Monitoring- und Datenqualitätsarchitektur

Zu unterscheiden sind Betriebsmonitoring und fachliches Energiemonitoring.

### Betriebsmonitoring

- strukturierte Logs mit CorrelationId, ImportId/CommitAttemptId, BuildingId und MeterId, ohne sensible Payloads;
- Metriken für API-Latenz, Fehlerquote, DB-Pool, Importbatches, Messwerte pro Sekunde, Aggregationslatenz und Reportjobs;
- Traces über API -> Application -> PostgreSQL/Object Storage;
- Health: Liveness nur Prozess; Readiness prüft erforderliche PostgreSQL-/TimescaleDB-Verbindung;
- OpenTelemetry-kompatible Instrumentierung, konkretes Backend bleibt Deploymententscheidung.

### Fachliches Monitoring

- erwartete Messintervalle je Meter-Version;
- Missing-Interval-Erkennung und Coverage;
- Duplicate-/Out-of-order-/Correction-Zähler;
- Plausibilitätsregeln je Messgröße und Einheit;
- Lastspitzen und Vergleich mit Baseline;
- Datenqualitätsbefunde als eigene persistente Findings mit Regelversion und Status.

## 13. Sicherheitsarchitektur für Phase 4 vorbereiten

Phase 3 implementiert keine vollständige Benutzerverwaltung, legt aber folgende Grenzen fest:

- `ICurrentActor`/Authorization-Hooks an Application Commands, noch ohne eigenes Identity-System;
- `tenant_id` nicht vorsorglich in jede Tabelle einbauen; Mandantenmodell wird in Phase 4 fachlich entschieden. IDs und Unique Constraints dürfen dessen Ergänzung aber nicht verhindern;
- TLS für API und PostgreSQL außerhalb lokaler Entwicklung erzwingen;
- Secrets ausschließlich über Environment/User Secrets/Secret Provider, nie im Repository;
- Encryption at Rest durch verwaltete Disk-/Database-/Object-Storage-Verschlüsselung; zusätzliche Feldverschlüsselung nur nach Datenklassifikation;
- SHA-256 für Integritätsnachweise von Dateien, nicht für Passwortspeicherung;
- Passwort-Hashing bleibt Aufgabe des späteren Identity Providers;
- Key Rotation und Key IDs in verschlüsselten Payloadmetadaten vorsehen;
- Audit append-only, zugriffsbeschränkt und frei von unnötigen personenbezogenen Inhalten;
- least-privilege DB-Rollen für Migration, API und Worker getrennt vorbereiten.

## 14. Bewertung der bestehenden Architektur

| Baustein | Status | Begründung / Maßnahme |
|---|---|---|
| Projekt-/Layergrenzen | **wiederverwendbar** | Clean-Architecture-Richtung stimmt; keine Solution-Neustrukturierung |
| `EnsetDbContext` in `Persistence` | **anzupassen** | als einzigen Context festlegen; zweite Datei `Infrastructure/DBContext.cs` nicht parallel verwenden |
| Domain Customer/Project/Building | **anzupassen** | direkte historisierte Portfoliozuordnung neben Project nötig |
| Meter/MeterReading-Schlüssel | **wiederverwendbar** | Composite Key und MeterNumber sind gute Basis; Revision/Qualität/Timescale ergänzen |
| `CalculationResult`/`KPIType` | **anzupassen** | definitions-/versionsbasierte KPI-Engine statt Enum als Erweiterungsgrenze |
| `BenchmarkDataset` | **anzupassen** | Quelle, Version, Segment und Gültigkeit fehlen |
| EF/Npgsql-Pakete und Migrationen | **wiederverwendbar** | echte Runtime-Konfiguration, Timescale-Migration und Integrationstests fehlen |
| `DatabaseImportWriter` | **anzupassen** | sicher blockierender Adapter ist richtige Grenze; transaktionalen Snapshot-Write implementieren |
| ImportCoordinator/Resolution/Commit/Gate | **wiederverwendbar** | nicht weiter perfektionieren; nur notwendige Payload-/Concurrency-Gates schließen |
| JSON-Reportrepository | **nicht mehr als Produktion erforderlich** | als Development/Test-Adapter behalten, produktiv EF/PostgreSQL |
| lokaler `FilePath` bei Document | **nicht mehr erforderlich** | durch ObjectKey plus Metadaten ersetzen |
| API/OpenAPI/NSwag | **wiederverwendbar** | neue operative Endpunkte nach gleichem Contractprinzip ergänzen |
| React Shell/Wizard | **wiederverwendbar** | P1 erste interne operative Ansichten; generierten Client kapseln |
| Worker-Konsole | **anzupassen** | später Generic Host für Aggregationen/Qualitätsjobs; kein Message Broker in Phase 3 |
| Historien-/KPI-/Reportingmodelle | **neu zu implementieren** | im aktuellen Code nicht ausreichend vorhanden |

## 15. Umsetzungsslices

### P0

1. Buildbaseline reparieren und einen kanonischen `EnsetDbContext` festlegen.
2. PostgreSQL-Konfiguration, Migrationstest und lokale TimescaleDB-Entwicklungsinstanz bereitstellen.
3. Core-Beziehungsmodell inklusive Building-Version und ExternalIdentifier migrieren.
4. Customer/Building Application Queries und Gebäudeliste/-detail API implementieren.
5. Meter/MeterVersion und Meter-API implementieren.
6. MeterReading-Hypertable, idempotente Batchpersistenz und Readings-/Series-API implementieren.
7. Tages-/Wochen-/Monats-/Jahresaggregate mit Coverage bereitstellen.
8. EnergySystemVersion, Energieträger und Document-Metadaten ergänzen.
9. KPI-Definitionen, Calculation Runs und P0-Kennzahlen implementieren.
10. `DatabaseImportWriter` auf diese getesteten Application-/Persistenzgrenzen anbinden.

### P1

1. Lastgang, Spitzen, Periodenvergleich, Plausibilität und Lückenerkennung.
2. Reportdefinitionen und CSV-/Excel-/PDF-Adapter.
3. Dashboard-Projektionen und erste interne React-Ansichten.
4. Energieausweis-/Sanierungsmaßnahmen-Modelle fachlich vertiefen.
5. Betriebs- und Datenqualitätsmonitoring vervollständigen.

### Definition of Done Phase 3

- PostgreSQL- und TimescaleDB-Migrationen laufen reproduzierbar gegen eine leere und eine aktualisierte Testdatenbank.
- Customer, Building, Meter und MeterReading werden transaktional/idempotent persistiert.
- Gebäudeliste, Gebäudedetail, Meter und Messreihen sind über versionierte API und NSwag nutzbar.
- Aggregationen liefern korrekte Ergebnisse einschließlich Zeitzonen-/Sommerzeittests und Coverage.
- Änderungen an Building, EnergySystem und Meter bleiben über Versionen nachvollziehbar.
- mindestens Verbrauch, Intensität, Peak, Coverage und CO2e sind versioniert/reproduzierbar berechnet.
- kein Import-, API- oder Workerpfad greift direkt an Application-Regeln vorbei auf Persistenz zu.
- Auth-/Mandanten-/Krypto-Hooks sind dokumentiert, ohne Phase-4-Funktionalität vorwegzunehmen.
# Stand 1.0 RC

EF Core, PostgreSQL-Migrationen, Geography und DataProduct-Persistenz sind implementiert. Der relationale Import-Writer ist weiterhin offen. Der tatsächliche Stand und die Risiken sind in [ARCHITECTURE_BASELINE_V1_0_RC.md](ARCHITECTURE_BASELINE_V1_0_RC.md) und [ARCHITECTURE_REVIEW_V1_0_RC.md](Decisions/ARCHITECTURE_REVIEW_V1_0_RC.md) festgehalten.

