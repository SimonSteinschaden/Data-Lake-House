# Architecture Review V2.0

**Datum:** 30. Juli 2026  
**Status:** Abschlussreview der MVP-Architekturphasen A bis D  
**Repository-Commit:** `c81f66b` zu Beginn des Reviews; zusätzliche lokale
Phasenänderungen wurden anhand des Arbeitsbaums bewertet.

Dieses Review bewertet die Umsetzung der
[Architecture Baseline V2.0](ARCHITECTURE_BASELINE_V2_0.md). Es ersetzt die
Baseline nicht. Aussagen beruhen auf Repositorystruktur, Quellcode, Tests,
ADRs und der unter [Referenzen](#referenzen) aufgeführten Dokumentation.

## 1. Executive Summary

Das ENSET Data Lake House hat nach den Phasen A bis D einen hohen
architektonischen MVP-Reifegrad erreicht. Die zentrale Verbesserung ist die
Trennung zwischen relationalem Write Model und kanonischem fachlichem Read
Model. Import, Persistenz und Historisierung bewahren Originaldaten;
bestätigte Kuration wird an einer Stelle priorisiert; Dashboard, CRUD,
Internal Data Products und LEB-Export konsumieren anschließend dieselben
Canonical Snapshots über `ICanonicalSnapshotReader`.

Die wichtigsten Erfolge sind:

- persistente, historisierte Übernahme relevanter Importfelder;
- originale Zählpunktnummer statt generierter Identifikatoren;
- zentrale Snapshot-Contracts für Customer, Building, Meter,
  Messwertzusammenfassung und EnergySystem;
- eine zentrale Jahreswertlogik ohne Hochrechnung;
- klare Trennung von Quality Level und anwendungsfallspezifischer Suitability;
- gemeinsame fachliche Quelle für CRUD, Dashboard, Products und LEB;
- identische LEB-Datengrundlage für Validate, CSV und Excel;
- Architekturtests gegen direkte Curated-Field- und Export-EF-Zugriffe.

Die Architekturqualität und Wartbarkeit sind für einen MVP gut. Application
Contracts, Infrastructure Adapter, API und React-Client besitzen erkennbare
Verantwortungsgrenzen. Das Domänenmodell deckt die Kerndomänen ab und
historisiert Building-Daten. Die Data-Lake-House-Eigenschaft ist fachlich
plausibel: Raw-Ablage, relationale qualitätsgesicherte Persistenz, kanonische
Projektionen und Data Products sind vorhanden. Die Zonen sind jedoch
überwiegend logisch und noch keine vollständig betrieblich isolierten
Lakehouse-Speicherstufen.

Die größten verbleibenden Risiken liegen nicht mehr in der fachlichen
Konsistenz, sondern in Performance, Persistenzisolation und Betriebsreife:
Der Portfolio-Reader arbeitet intern N+1-artig, Customer- und
EnergySystem-Snapshots sind nicht dauerhaft materialisiert,
`includeDeleted` ist im kanonischen Lesepfad eingeschränkt, ein fachliches
BuildingCondition-Modell fehlt, reale PostgreSQL-/TimescaleDB-End-to-End-
Nachweise sind begrenzt und Observability sowie produktive Deployment-
Härtung sind nicht vollständig belegt. Zusätzlich besteht die bekannte
NU1903-Sicherheitswarnung.

**MVP-Einschätzung:** architektonisch stabil und für fachliche MVP-Abnahmen
geeignet; für produktiven Dauerbetrieb sind Betriebs-, Security-, Last- und
PostgreSQL-Integrationsnachweise erforderlich. Gesamt-Reifegrad: **82 %**.

## 2. Umsetzung der Architekturphasen

### Phase A – Importpersistenz

**Status: abgeschlossen**

| Aspekt | Bewertung |
|---|---|
| BuildingVersion | Import erzeugt und aktualisiert aktive Versionen; Vorgänger werden zeitlich geschlossen. |
| MeterNumber | Die originale LEB-`ZId` wird als `Meter.MeterNumber` erhalten; `Meter.Id` bleibt technische GUID. |
| AnnualTotal | Wert, Einheit und Bezugsjahr werden persistiert; ungültige beziehungsweise negative Werte erzeugen Issues. |
| ReadingType | LEB- und Lastprofil-Zeitreihen werden kontrolliert als `IntervalValue` übernommen. |
| Quantity | Ableitung nur bei eindeutiger Einheit; sonst `Unknown` plus Warnung. |
| IntervalSeconds | Nur explizit oder aus nachweislich konstantem Raster; gemischte Intervalle bleiben null. |
| Historisierung | Building-Attribute und Adresse werden über BuildingVersion historisiert. |
| Persistenz | `DatabaseImportWriter` schreibt über den gemeinsamen Importpfad; Analyse und Commit bleiben getrennt. |

Die Migration `PersistPhaseAImportFields` war für
`AnnualValueReferenceYear`, `Address.City` und nullable Building-Enums
erforderlich. Nicht vorhandene Werte werden nicht geraten.

### Phase B – Canonical Snapshot

**Status: abgeschlossen, mit Materialisierungseinschränkung**

Implementiert sind Snapshot-Contracts, `ICanonicalSnapshotReader`,
`EfCanonicalSnapshotReader`, zentrale Feldpriorität, `SnapshotQuality`,
`SnapshotSuitability`, `CanonicalReadingSummary`,
`CanonicalAnnualValue` und `CanonicalVersion`. Die fachlichen Internal Data
Products lesen über den Snapshot-Reader.

Quality Level verwendet Bronze, Silver und Gold. Suitability für LEB,
Navigator, Benchmark und ISO 50001 bleibt davon getrennt. Jahreswerte haben
die Status `CompleteYear`, `IncompleteYear` und `NotAvailable`; Teiljahre
werden nicht hochgerechnet.

Building- und MeteringPoint-Strukturen können vorhandene
`GoldProfileVersion`-Informationen nutzen. Customer- und
EnergySystem-Snapshots werden deterministisch on demand erzeugt, aber nicht
dauerhaft materialisiert. Phase B führte bewusst keine zweite
Snapshot-Persistenz ein.

### Phase C – CRUD und Dashboard

**Status: abgeschlossen; Frontend-Wiederverwendung teilweise**

`EfEntityReadService` verwendet für fachliche Customer-, Building- und
Meter-Werte `ICanonicalSnapshotReader`. Listenfilter und Sortierung basieren
auf den angezeigten kanonischen Werten. Direkte EF-Zugriffe verbleiben für
Audit-/Concurrency-Metadaten und Rohmesswert-Pagination. Dashboard und
fachliche Products konsumieren dieselbe Snapshot-Schicht.

REST-DTOs wurden additiv um Quality Level, Gemeinde-, Jahreswert- und
Messwertfelder ergänzt. Die originalen Identifikatoren bleiben von
technischen GUIDs getrennt.

`CanonicalDisplays.tsx` stellt zentrale Customer-, Building-, Meter-,
Quality-, Jahreswert-, Zeitraum- und Leerwertdarstellungen bereit. Die
aktuellen Seiten verwenden diese Komponenten jedoch noch nicht durchgehend;
einzelne ältere Texte wie „Gold-Reife“ und lokale Formatierungen bestehen
weiter. Die fachlichen Backendwerte sind vereinheitlicht, die vollständige
visuelle Wiederverwendung ist daher **teilweise** umgesetzt.

### Phase D – LEB-Export

**Status: abgeschlossen**

`EfNoeLebContractBuilder` hängt ausschließlich von
`ICanonicalSnapshotReader` ab. `LebExportDataset` ist die gemeinsame
fachliche Projektion für Validate, CSV und Excel. Die Serializer konsumieren
denselben unveränderten `NoeLebExportContractV1`.

Der Export liest keine fachlichen EF-Entities und keine
`CuratedFieldValues`. Originale MeterNumber und optionales Meter.Name bleiben
getrennt. Quality Level ist keine globale Exportfreigabe;
`LebSuitability` entscheidet anwendungsfallspezifisch. Bronze und Silver
werden nicht pauschal ausgeschlossen. `NotSuitable` erzeugt einen
blockierenden Validierungsfehler. Contract V1, Spaltenfolge und Formate
bleiben stabil.

## 3. Architekturentwicklung

### Vorher

```text
Import
  -> EF-Entities
  -> individuelle Product-/CRUD-Projektionen
  -> separater LEB-EF-Builder
```

Feldpriorität, Datenreife, Jahreswerte und Zuordnungen konnten je Verbraucher
unterschiedlich abgeleitet werden. Direkte Zugriffe auf relationale Originale
oder CuratedFieldValues machten Abweichungen möglich.

### Heute

```text
Import
  -> relationale Persistenz und Historisierung
  -> bestätigte zentrale Kuration
  -> Canonical Snapshot
  -> ICanonicalSnapshotReader
       |-- Dashboard
       |-- CRUD-Listen und -Details
       |-- Internal Data Products
       `-- LebExportDataset -> Validate -> CSV / Excel
```

Gelöst wurden insbesondere konkurrierende fachliche Wahrheiten, mehrfach
implementierte Jahreswertregeln, die Vermischung von Quality und Suitability,
abweichende Customer-/Building-/Meter-Zuordnungen und direkte fachliche
Exportprojektionen aus EF.

## 4. Architekturprinzipien

| Prinzip | Status | Bewertung |
|---|---|---|
| Single Source of Truth | Umgesetzt | Downstream-Fachwerte laufen über `ICanonicalSnapshotReader`. |
| Preserve Originals | Umgesetzt | Originale MeterNumber und Importwerte bleiben erhalten; Kuration überschreibt sie nicht physisch. |
| Central Curation | Umgesetzt | Bestätigte aktuelle CuratedFieldValues werden nur im Snapshot-Builder priorisiert. |
| No Guessing | Umgesetzt | Unbekannte Einheiten, Intervalle und Werte bleiben Unknown/null und erzeugen Issues. |
| Quality != Suitability | Umgesetzt | Getrennte Contracts und LEB-Validierung; Gold ist keine Freigabe. |
| One Annual Value Logic | Umgesetzt | `CanonicalAnnualValue` und `CanonicalReadingSummary`; keine Hochrechnung. |
| Canonical Snapshots | Umgesetzt | Contracts und Reader sind gemeinsame Read-Model-Grenze. |
| Technical IDs | Umgesetzt | GUIDs bleiben intern; fachliche Nummern besitzen eigene Felder. |
| Products read no raw tables | Weitgehend umgesetzt | Fachliche Products lesen Snapshots; ImportQualityProduct ist dokumentierte technische Ausnahme. |
| Exports are projections | Umgesetzt | LEB ist eine Projektion desselben CanonicalSnapshotSet. |
| Persistierte Snapshot-Isolation | Teilweise | Customer und EnergySystem sind deterministische, nicht materialisierte Projektionen. |

## 5. Data Lineage

```text
Datei
  -> Reader und Importmodelle
  -> Validation / Duplication / Resolution / WriteGate
  -> relationale Persistenz und ImportReport
  -> bestätigte Kuration
  -> Canonical Snapshot
  -> Internal Products / CRUD / Dashboard
  -> REST
  -> React
  -> LEB-Projektion und Serializer
```

Originalwerte werden in den relationalen Modellen erhalten.
BuildingVersion sorgt für zeitliche Nachvollziehbarkeit; ImportReport,
ImportIssue und Auditinformationen dokumentieren den Workflow.

In den geprüften fachlichen Product-, CRUD- und LEB-Pfaden bestehen keine
separaten Jahreswertberechnungen. Der LEB-Export enthält keine direkte
CuratedField- oder fachliche EF-Abhängigkeit. Technische EF-Lesewege für
Rohmesswerte, Audit und Importqualität sind begründet. Im Frontend bestehen
noch redundante Formatierungen, aber keine eigenständige fachliche
Jahreswertberechnung.

## 6. Domänenmodell

| Bereich | Bewertung |
|---|---|
| Customer | Klare fachliche Nummer und Kontaktdaten; Snapshot on demand, noch nicht materialisiert. |
| Building | Eigenständige Identität, Customer-Zuordnung und versionierte Fachdaten. |
| BuildingVersion | Gute Historisierung von Nutzung, Kategorie, Adresse, Bau- und Flächendaten. BuildingCondition fehlt. |
| EnergySystem | Kerndaten und Building-Zuordnung vorhanden; kanonische Projektion schlanker und nicht materialisiert. |
| Meter | Technische ID, originale MeterNumber und Name sauber getrennt; Medium, Quantity, Unit und Richtung typisiert. |
| MeterReading | Zeitstempel, Wert, ReadingType, IntervalSeconds, Qualität und Herkunft unterstützen Zeitreihen- und Qualitätsauswertung. |
| Import | Analyse, Entscheidung und Commit sind getrennt und wiederaufnehmbar modelliert. |
| ImportIssue | Typisierte Severity, Codes, Resolution und blockierende Semantik sind nachvollziehbar. |
| ImportReport | Workflow-, Audit- und Statistikmodell; kein fachliches Data Product. |
| Data Products | Explizite fachliche Contracts mit API-Service statt UI-spezifischer Ad-hoc-Abfragen. |
| Canonical Snapshots | Starke Read-Model-Grenze; Materialisierungsstrategie noch nicht für alle Entitäten vollständig. |

## 7. Qualitätsmodell

- **Quality Level:** Bronze/Silver/Gold beschreibt allgemeine Datenqualität.
- **Suitability:** LEB, Navigator, Benchmark und ISO 50001 werden getrennt
  beurteilt.
- **Validation:** Import- und LEB-Regeln erzeugen strukturierte Ergebnisse.
- **Warnings:** Nicht blockierende Defizite bleiben sichtbar.
- **Blocking Errors:** Verhindern Commit beziehungsweise Export.
- **AnnualValueStatus:** verhindert die Darstellung eines Teiljahres als
  Jahreswert.
- **Measurement Summary:** liefert Count, Zeitraum, Einheit, ReadingType,
  Quantity, Intervall, Quality-Counts, Vollständigkeit und Jahreswert.

Das Modell ist für den MVP konsistent. Die Suitability-Regeln sind bewusst
schlank und noch keine generische, administrierbare Rule Engine.

## 8. API

Die API trennt Controller, Application Ports und Infrastructure Adapter.
Vorhanden sind versionierte `/api/v1`-Endpunkte für Import, CRUD, Internal
Data Products und LEB. ProblemDetails, HTTP 422 für blockierende
Exportvalidierung, JWT-Bearer-Authentifizierung, Policies und
`IDataAccessScope` sind implementiert.

CRUD-Verträge wurden kompatibel additiv erweitert. Product- und
LEB-Verträge besitzen stabile fachliche Namen. Validate und beide
LEB-Downloads verwenden dieselbe Pipeline. Verbesserungsbedarf besteht bei
vollständigen End-to-End-Contracttests gegen einen realen Host und bei der
expliziten API-Dokumentation neuer additiver Felder.

## 9. Frontend

React/Vite deckt Import Wizard, Dashboard, CRUD-Seiten, Curation,
Data-Product-Ansichten und Exporte ab. Services kapseln HTTP-Aufrufe; das
Frontend liest keine Persistenz direkt.

Dashboard und Exportseite verwenden serverseitig berechnete Fachwerte.
Listen und Detailseiten erhalten kanonisch erzeugte REST-Werte. Gemeinsame
Display-Komponenten existieren, sind aber noch nicht flächendeckend
eingebunden. Veraltete UI-Terminologie und lokale Leerwert-/Zeitraumformatter
reduzieren die visuelle Konsistenz. Dedizierte Frontend-Unit-, Accessibility-
und Browser-End-to-End-Tests sind im aktuellen Testprojekt nicht
nachgewiesen.

## 10. Datenbank

PostgreSQL/Npgsql und EF Core bilden die operative Persistenz. Globale
Soft-Delete-Filter und PostgreSQL-`xmin` als RowVersion unterstützen
Lösch- und Concurrency-Semantik. BuildingVersion, GoldProfileVersion,
CuratedFieldValue, ImportReport und Auditmodelle liefern unterschiedliche,
klar benannte Historien.

Das Repository enthält 14 handgeschriebene EF-Migrationen einschließlich der
minimalen Phase-A-Migration. Phase B bis D benötigten keine Migration.

MeterReading ist TimescaleDB-kompatibel. Ein produktiv verifizierter
Hypertable-Betrieb, Retention, Kompression und Lastnachweis sind jedoch nicht
belegt. Die Architektur ist daher PostgreSQL-produktiv orientiert, bei
TimescaleDB aber noch integrations- und betriebsseitig nachzuweisen.

## 11. Tests

Zum Abschluss der Phase D wurden folgende Ergebnisse nachgewiesen:

| Prüfung | Ergebnis |
|---|---|
| xUnit | 210/210 erfolgreich |
| API Release-Build | erfolgreich |
| Worker Release-Build | erfolgreich |
| Frontend-Lint | erfolgreich |
| Frontend-Produktionsbuild | erfolgreich |
| `git diff --check` | erfolgreich |

Die Tests decken Importmapping und -persistenz, Autorisierung, Canonical
Snapshots, Internal-Product-Contracts, CRUD-Architektur, LEB-Contract,
Serializer, Validierung und verbotene Architekturabhängigkeiten ab. Das ist
eine gute MVP-Abdeckung der kritischen Fachregeln.

Qualitative Lücken sind reale PostgreSQL-/TimescaleDB-End-to-End-Tests,
Last-/Speichertests großer Portfolios, Browser-End-to-End-Tests,
Accessibility-Tests, Failure-/Recovery-Szenarien und produktionsnahe
Securitytests.

## 12. Dokumentation

Die Baseline definiert Begriffe, Schichten und den Stand nach Phase A/B.
Data Lineage beschreibt Feldwege bis UI und LEB. Eigene Dokumente erläutern
Canonical Snapshots, Internal Products, CRUD-Vereinheitlichung und
LEB-Projektion. Die fünf ADRs halten wesentliche Entscheidungen fest.
PlantUML-Diagramme dokumentieren Komponenten, Domain, Import, REST, Worker,
Web, Product-Architektur und Datenbank.

Die Dokumentationsbreite ist hoch. Einzelne ältere Dokumente – insbesondere
frühere Frontend-/Data-Lake-House-Texte und die Baseline-Aussagen „Phase C/D
offen“ – spiegeln bewusst oder historisch einen älteren Stand. Ohne
Versionskontext wirken sie widersprüchlich. Eine spätere konsolidierte
Dokumentationsnavigation sollte aktuelle Reviews und historische Baselines
klar kennzeichnen.

## 13. Bekannte Einschränkungen

| Priorität | Typ | Einschränkung | Auswirkung |
|---|---|---|---|
| P1 | Performance | Portfolio-Reader ruft Entity-Snapshots intern einzeln ab (N+1-artig). | Skalierungsrisiko bei großen Portfolios und Exporten. |
| P1 | Architektur | Customer-Snapshots sind nicht dauerhaft materialisiert. | Keine vollständige Isolation von relationalen Änderungen und keine eigenständige historische Customer-Snapshotfolge. |
| P1 | Architektur | EnergySystem-Snapshots sind nicht dauerhaft materialisiert. | Gleiche Isolationseinschränkung; Projektion bleibt deterministisch. |
| P1 | Betrieb | Reale PostgreSQL-/TimescaleDB-E2E- und Lastnachweise fehlen. | Produktive Belastbarkeit ist nicht vollständig belegt. |
| P1 | Technisch/Security | NU1903 für `System.Security.Cryptography.Xml` 9.0.15. | Bekannte High-Severity-Paketwarnungen müssen vor Produktion behoben werden. |
| P2 | Funktional | `includeDeleted` kann gelöschte fachliche Snapshotwerte nicht vollständig liefern. | Administrative Historienansichten sind eingeschränkt. |
| P2 | Domain | Fachlich geeignetes BuildingCondition-Modell fehlt. | Gebäudezustand kann nicht konsistent importiert oder projiziert werden. |
| P2 | Frontend | Zentrale CanonicalDisplays werden nicht überall verwendet. | Uneinheitliche Begriffe und Formatierung trotz konsistenter Backendwerte. |
| P2 | Betrieb | Monitoring, Metriken, Alerting, Backup/Restore und Retention sind nicht vollständig nachgewiesen. | Eingeschränkte Produktionsbetriebsreife. |
| P2 | Datenplattform | TimescaleDB-Hypertable-Betrieb ist nicht verifiziert. | Zeitreihenoptimierung bleibt potenziell ungenutzt. |

## 14. Gesamtbewertung

| Bereich | Bewertung | Begründung |
|---|---|---|
| Architektur | ★★★★☆ | Klare Schichten und zentrale Fachquelle; Materialisierung und Betrieb offen. |
| Domänenmodell | ★★★★☆ | Gute Kerndomäne und Historisierung; BuildingCondition fehlt. |
| Persistenz | ★★★★☆ | EF/PostgreSQL, Concurrency, Audit und Migrationen solide; produktive E2E-Nachweise fehlen. |
| Import | ★★★★★ | Mehrstufiger, validierter und nachvollziehbarer Workflow mit Originalerhalt. |
| Canonical Snapshots | ★★★★☆ | Fachlich zentrale Contracts; Customer/EnergySystem nicht materialisiert. |
| CRUD | ★★★★☆ | Kanonische Backendreads; UI-Wiederverwendung noch nicht vollständig. |
| Internal Data Products | ★★★★☆ | Klare Contracts und zentrale Quelle; kein eigener persistierter Produktlebenszyklus. |
| LEB | ★★★★★ | Gemeinsames Dataset, stabiler Contract und getrennte Suitability. |
| REST | ★★★★☆ | Versionierung, Policies und konsistente Contracts; mehr Host-E2E-Tests sinnvoll. |
| Frontend | ★★★☆☆ | Breite MVP-Funktionalität; Komponentenreuse und automatisierte UI-Tests ausbaufähig. |
| Tests | ★★★★☆ | 210 fachlich relevante Tests; reale Plattform- und Browser-E2E-Lücken. |
| Dokumentation | ★★★★☆ | Umfangreich und entscheidungsorientiert; ältere Dokumente teilweise überholt. |
| Performance | ★★★☆☆ | Funktional ausreichend; Portfolio-N+1 und fehlende Lastnachweise. |
| Erweiterbarkeit | ★★★★☆ | Ports, Contracts und Projektionen erleichtern neue Products/Exports. |
| Wartbarkeit | ★★★★☆ | Fachlogik zentralisiert und Architekturtests vorhanden. |
| Betrieb | ★★★☆☆ | Hosts und Security-Basis vorhanden; Observability und Plattformnachweis offen. |

## 15. MVP-Reifegrad

**Gesamtschätzung: 82 %**

| Dimension | Reife |
|---|---:|
| Domäne | 85 % |
| Persistenz | 82 % |
| Import | 92 % |
| Snapshots | 82 % |
| CRUD | 84 % |
| Products | 85 % |
| LEB | 92 % |
| REST | 84 % |
| Frontend | 72 % |
| Tests | 84 % |
| Dokumentation | 86 % |
| Deployment | 62 % |
| Security | 72 % |
| Monitoring | 48 % |

Die Prozentangabe bewertet Architektur und Nachweisbarkeit, nicht
Codeumfang. Fachliche Konsistenz, Import, Products und LEB sind weit
fortgeschritten. Der Abstand zu 100 % entsteht vor allem durch produktive
Plattformtests, Performance, Snapshotmaterialisierung, UI-Testtiefe,
Dependency-Härtung, Deployment und Observability.

## 16. Roadmap nach MVP

### Priorität P1

1. NU1903-Abhängigkeit aktualisieren und vollständigen Dependency-/Container-
   Securityscan etablieren.
2. PostgreSQL- und TimescaleDB-End-to-End-Tests einschließlich Migration,
   Concurrency, Berechtigungen und Restore ausführen.
3. Portfolio-Reader mit gebündelten Snapshotqueries optimieren und durch
   Query-Count- sowie Lasttests absichern.
4. Eine kontrollierte Materialisierungsstrategie für Customer und
   EnergySystem auf der bestehenden GoldProfileVersion-/Snapshotarchitektur
   entscheiden.
5. Authentifizierung und Autorisierung produktiv härten: realen Identity
   Provider, Secret Management, Token-Konfiguration und Penetrationstests
   nachweisen.
6. Health Checks, strukturierte Logs, Tracing, Metriken, Alerting,
   Backup/Restore und Runbooks vervollständigen.
7. Browser-End-to-End-, Accessibility- und kritische Frontend-Komponententests
   ergänzen.

### Priorität P2

1. `includeDeleted` fachlich und technisch in der kanonischen
   Historienabfrage lösen.
2. BuildingCondition als abgestimmtes Domainkonzept modellieren.
3. Gemeinsame React-Display-Komponenten flächendeckend verwenden und alte
   „Gold-Reife“-Terminologie bereinigen.
4. TimescaleDB-Hypertables, Retention und Kompression produktiv verifizieren.
5. Navigator-, Data-Space- und Marketplace-Projektionen auf der bestehenden
   Canonical-/Product-Schicht aufbauen.
6. Analytics, Benchmarks, Reporting und Optimierungsmodule als versionierte
   Data Products ergänzen.
7. Historische Dokumente kennzeichnen und eine zentrale aktuelle
   Dokumentationsnavigation etablieren.

## 17. Fazit

Die Architektur ist heute eine konsistente, fachlich ausgerichtete
Data-Lake-House-Architektur für den MVP. Sie besitzt keine vollständig
physisch getrennten Lakehouse-Zonen, bildet aber Ingestion, Raw-Ablage,
qualitätsgesicherte relationale Persistenz, Kuration, kanonische Projektionen
und konsumierbare Data Products nachvollziehbar ab.

Canonical Snapshot ist für die betrachteten fachlichen Downstream-Pfade
tatsächlich die Single Source of Truth. Dashboard, CRUD, Internal Data
Products und LEB sind im Backend fachlich vereinheitlicht. Die
On-demand-Projektion von Customer und EnergySystem schwächt die
Persistenzisolation, erzeugt aber keine zweite fachliche Wahrheit.

Das MVP ist architektonisch stabil und für fachliche Pilotierung geeignet.
Einen uneingeschränkten produktiven Einsatz verhindern derzeit vor allem die
offene Paket-Sicherheitswarnung, fehlende produktionsnahe PostgreSQL-/
TimescaleDB-, Last- und Recovery-Nachweise, die Portfolio-Performance sowie
noch unvollständige Observability und Deployment-Härtung. Diese Themen
erfordern keine Wiederholung der Phasen A bis D, sondern gezielte
Produktionsreife-Arbeit.

## Referenzen

- [Architecture Baseline V2.0](ARCHITECTURE_BASELINE_V2_0.md)
- [Data Lineage Analysis V1.0](DATA_LINEAGE_ANALYSIS_V1_0.md)
- [Canonical Snapshots](docs/canonical-snapshots.md)
- [Internal Data Products](docs/internal-data-products.md)
- [Unified CRUD Read Models](docs/unified-crud-read-models.md)
- [LEB Export – Canonical Projection](docs/leb-export-canonical-projection.md)
- [ADR: Canonical Snapshot als Single Source of Truth](docs/adr/ADR_CANONICAL_SNAPSHOT_AS_SINGLE_SOURCE_OF_TRUTH.md)
- [ADR: Internal Data Products](docs/adr/ADR_INTERNAL_DATA_PRODUCTS.md)
- [ADR: LEB als erstes External Data Product](docs/adr/ADR_LEB_AS_FIRST_EXTERNAL_DATA_PRODUCT.md)
- [ADR: Unified CRUD and Product Projections](docs/adr/ADR_UNIFIED_CRUD_AND_PRODUCT_PROJECTIONS.md)
- [ADR: LEB Export from Canonical Data](docs/adr/ADR_LEB_EXPORT_FROM_CANONICAL_DATA.md)
- [MVP Overview](docs/00_Overview.md)
- [Architecture](docs/01_Architecture.md)
- [Data Lake House](docs/03_Data_Lake_House.md)
- [API](docs/06_API.md)
- [Frontend](docs/07_Frontend.md)
- [Authorization](docs/13_User_Tenant_Authorization.md)
- [Roadmap](docs/11_Roadmap.md)
