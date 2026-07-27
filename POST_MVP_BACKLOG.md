# ENSET Post-MVP Backlog

Stand: 27. Juli 2026

## 1. Zweck und Scope-Abgrenzung

Dieses Dokument sammelt Architektur-, Governance- und Erweiterungsthemen, die
bewusst **nicht** Bestandteil des MVP v1.0 sind. Es ist ein Scope-Schutz für
den laufenden MVP und keine Zusage, alle aufgeführten Themen umzusetzen.

Der Backlog unterscheidet zwischen:

- bestätigten Post-MVP-Epics mit erkennbarem fachlichem Nutzen;
- Architekturentscheidungen, die vor einer Umsetzung geklärt werden müssen;
- technischen Schulden, die nach dem MVP bewertet werden;
- optionalen fachlichen Erweiterungen, deren Bedarf noch nachzuweisen ist.

Maßgeblicher Ist-Stand sind der aktuelle Code, die Architecture Baseline 1.0
RC, die Data Product Engine 1.0 RC und das aktuelle Architecture Review.
Historische Dokumente werden nur als Entscheidungs- und Herkunftsnachweis
verwendet. Aussagen, die dem aktuellen Code widersprechen, gelten nicht als
Implementierungsnachweis.

Dieses Dokument erweitert den MVP nicht. Post-MVP-Epics dürfen erst begonnen
werden, wenn dadurch keine noch offenen MVP-Kernprozesse verzögert werden.

## 2. Verbindlicher MVP-Fokus

Der MVP v1.0 bleibt auf folgende Ergebnisse begrenzt:

- Reporting auf Basis bereits vorhandener Daten;
- Dashboard und Visualisierung vorhandener Messwerte und Lastkurven;
- erste fachlich nutzbare Data Products;
- Erzeugung über die bestehenden Generatoren
  `METER_CONSUMPTION_SUMMARY` und `BUILDING_ENERGY_PROFILE`;
- Nutzung und Stabilisierung der vorhandenen Import-, Domain-, API- und
  React-Grundlagen;
- Darstellung von Importstatus und vorhandener Datenqualität;
- notwendige Customer-, Building-, Meter- und Data-Product-Ansichten;
- notwendige API-Endpunkte für diese MVP-Flows;
- Fehlerbehebungen und Tests für einen durchgängigen MVP-End-to-End-Flow;
- produktionsnotwendige Basisfunktionen, die bereits in der freigegebenen
  MVP-Roadmap stehen, insbesondere reale Authentifizierung und
  Datenbank-/HTTP-End-to-End-Tests.

Diese Arbeiten sind **kein** Post-MVP-Backlog:

- Fertigstellung oder Korrektur des bestehenden Import- und Commit-Flows;
- produktionsrelevante Stabilisierung des relationalen Writers;
- MVP-Authentifizierung und serverseitige Autorisierung;
- vorhandene Dashboards, Reports und Data-Product-Generatoren;
- Integrationstests für freigegebene MVP-Kernprozesse;
- Beseitigung von Fehlern, die vorhandene Daten unzugänglich machen.

## 3. Post-MVP-Leitprinzipien

### 3.1 Fachliche Domäne und Datenzustand sind orthogonal

`Building`, `MeteringPoint`, `Meter`, `EnergySystem` oder `Organization` sind
fachliche Objekte. Raw, Curated, analytisch und publiziert sind Zustände oder
Repräsentationen von Daten. Ein Domänenobjekt wird nicht allein deshalb der
„Datenerfassung“ zugeordnet, weil Daten über dieses Objekt importiert werden.

### 3.2 Zielbild: fünf fachliche Ebenen

```text
Sources & Acquisition
  → Canonical Domain
  → Curation & Semantic Integration
  → Analytical Models & Features
  → Data Products & Experiences
```

Governance, Security, Quality, Lineage und Operations wirken
querschnittlich über alle Ebenen.

Die fünf Ebenen sind zunächst logische Verantwortungsgrenzen. Aus ihnen folgt
keine Pflicht zu fünf Datenbanken, Microservices oder physischen
Bronze-/Silver-/Gold-Speichern.

### 3.3 Gemeinsames kanonisches Modell, getrennte Quellkontexte

LEB, CRM-Excel, CSV-Lastprofile und spätere Quellen dürfen
eigene Reader, Regeln und Benutzeroberflächen besitzen. Sie verwenden nach
kontrollierter Identitätsauflösung das gemeinsame kanonische Modell.
Quellübergreifende Zusammenführungen erfolgen nur anhand gespeicherter
externer Referenzen oder expliziter Benutzerentscheidungen.

### 3.4 Data Products sind verantwortete Verträge

Ein KPI, `CalculationResult`, `BenchmarkDataset`, Report oder Dashboard ist
nicht automatisch ein Data Product. Ein veröffentlichtes Data Product
benötigt mindestens Zweck, Owner, Scope, Version, Vertrag, Qualität,
Lineage, Zugriffspolitik und reproduzierbaren Erzeugungslauf.

### 3.5 Modularisierung folgt fachlichem Bedarf

ENSET bleibt zunächst ein modular strukturierter Monolith. Neue Deployables,
Bounded Contexts oder Plattformdienste entstehen erst, wenn ein konkreter
Vertrag, unabhängiger Lebenszyklus oder Betriebsbedarf nachgewiesen ist.

## 4. Backlog-Epics

### EPIC PM-01 – Fachliche Fünf-Ebenen-Zielarchitektur

- **Ziel:** Ein konsistentes ENSET-Universe-Zielbild definieren, das
  Domänenobjekte, Datenzustände, analytische Artefakte und Data Products
  eindeutig unterscheidet.
- **Ausgangslage:** Clean-Architecture-Projektgrenzen, Raw-Zone, relationaler
  Fachbestand und Data Product Engine bestehen. Dokumente verwenden teilweise
  parallel die Begriffe Domain, Data Lake, Raw/Silver/Gold und Data Product.
- **Umfang:** Verantwortungen und Übergabeverträge der fünf Ebenen;
  Zuordnung bestehender Modelle; querschnittliche Governance-Ebene;
  konsolidierte Diagramme, Glossar und Architecture Decision Records.
- **Nicht enthalten:** Aufteilung in Microservices; neue Speicherzonen;
  Migration produktiver Daten; Änderungen an MVP-Generatoren.
- **Abhängigkeiten:** Abschluss und Baseline des MVP; geklärte Begriffe aus
  Abschnitt 5.
- **Akzeptanzkriterien:** Ein freigegebenes Zielbild; jedes zentrale Modell
  ist fachlich eingeordnet; Übergänge und Verantwortliche sind beschrieben;
  widersprüchliche Architekturtexte sind ersetzt oder als historisch markiert.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

### EPIC PM-02 – Organization, fachlicher Scope und CRM-Integration

- **Ziel:** `Organization` als langfristigen organisatorischen Kontext
  einführen und die überladenen Bedeutungen von `Customer` auflösen.
- **Ausgangslage:** Customer steuert heute unter anderem Zuordnung und
  Autorisierung. Der LEB-Kontext transportiert Gemeinden kompatibel als
  Customer, benennt `Organization` aber bereits als langfristige Grenze.
- **Umfang:** Organization-Typen wie Municipality, Company, PrivateOwner und
  optional HousingAssociation; Rollen von Customer, Eigentümer, Betreiber,
  Mandant und Vertragspartner; primärer Scope; externe Identitäten;
  Vorstudie zur Twenty-CRM-Integration; führende Systeme,
  Synchronisationsstatus und Konfliktregeln.
- **Nicht enthalten:** Vollständiges CRM in ENSET; ungeprüfte bidirektionale
  Synchronisation; automatische Zusammenführung anhand bloßer Namensähnlichkeit.
- **Abhängigkeiten:** PM-01; Auth-/Tenant-Zielbild; fachliche CRM-Verantwortung.
- **Akzeptanzkriterien:** Context Map; Verantwortungsmatrix pro Attribut;
  definierte externe Schlüssel; Konflikt- und Löschregeln; Migrationsplan von
  Customer ohne Big-Bang-Umstellung.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

### EPIC PM-03 – Historisierte fachliche Assignments

- **Ziel:** Beziehungen zwischen Organisationen, Assets, Messpunkten und
  Teilnehmern explizit, rollenbasiert und zeitlich nachvollziehbar modellieren.
- **Ausgangslage:** `CustomerBuildingAssignment`, Building-Versionen und
  einzelne Scope Assignments existieren. Ein einheitliches Modell für
  Organization, MeteringPoint, Meter und Energy Community fehlt.
- **Umfang:** Assignments für Organization–Building, Customer–Building,
  Building–MeteringPoint, MeteringPoint–Meter,
  EnergyCommunity–MeteringPoint und Participant–MeteringPoint; Rolle,
  `ValidFrom`/`ValidTo`, Herkunft, Vertrauensniveau, primärer Scope und
  Mehrfachzuordnung.
- **Nicht enthalten:** Automatische Beziehungserfindung; vollständiges Event
  Sourcing; pauschale bitemporale Modellierung aller Entities.
- **Abhängigkeiten:** PM-02; MeteringPoint-Entscheidung aus Abschnitt 5;
  PM-05 für Provenance.
- **Akzeptanzkriterien:** Kardinalitäten und Invarianten sind dokumentiert;
  Zeitüberschneidungen sind geregelt; jede automatische Empfehlung bleibt
  auditierbar; Migrationsstrategie für bestehende Assignments liegt vor.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

Verbindliche Invariante für das spätere Modell:

> Ein `MeteringPoint` darf nur bestehen, wenn mindestens eine fachliche
> Zuordnung oder belastbare Identifikation vorhanden ist.

Mindestens eine Bedingung muss erfüllt sein:

- Zuordnung zu einem Customer beziehungsweise später einer Organization;
- Zuordnung zu einem Building;
- Zuordnung zu einer Energy Community;
- identifizierter oder extern referenzierter Participant.

Ein technisch eingelesener, aber fachlich unidentifizierter Datensatz bleibt
in Acquisition/Curation und wird nicht als kanonischer MeteringPoint
publiziert.

### EPIC PM-04 – Semantic & Analytical Models

- **Ziel:** Wiederverwendbare fachliche Berechnungs- und Bedeutungsmodelle
  zwischen Curated Domain und Data Products etablieren.
- **Ausgangslage:** Die Data Product Engine interpretiert bereits ReadingType,
  Quantity, Unit, Direction, IntervalSeconds und Quality. Zwei Generatoren
  besitzen nutzbare Berechnungen; eine umfassende zentrale Semantik fehlt.
- **Umfang:** Definitionen für Verbrauch, Erzeugung, Leistung,
  Energieflussrichtung, Zeitintervalle, Aggregationen, Einheiten,
  Flächen/Bezugsgrößen, Wetter-/Gradtagsbereinigung, Emissionsfaktoren,
  Kosten-/Tariflogik, Kennzahlen, Benchmark-Grundgesamtheiten und
  wiederverwendbare Features.
- **Nicht enthalten:** Ungeprüfte Formeln aus der Datenbank; sofortiger
  Knowledge Graph; regulatorische Konformitätszusage; neue MVP-KPIs.
- **Abhängigkeiten:** PM-01; PM-03; fachliche Methodik-Owner.
- **Akzeptanzkriterien:** Versionierte Definitionen und Calculator-Verträge;
  Einheit und Zeitraum sind explizit; Qualität/Coverage werden weitergegeben;
  Data-Product-Generatoren duplizieren keine zentrale Berechnungslogik.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

### EPIC PM-05 – Data Lineage und Data Quality Framework

- **Ziel:** Herkunft, Transformation und Vertrauenswürdigkeit von der Quelle
  bis zum Data Product nachvollziehbar machen.
- **Ausgangslage:** ImportReport, Issues, Audit, Raw-Archivierung,
  `ImportedMeterReading`, Quality-Werte und Generation Runs liefern
  Teilnachweise. Eine durchgängige Provenance-Kette und zentral versionierte
  Qualitätsregeln fehlen.
- **Umfang:** Source-/Snapshot-Identität; Feld- und Datensatz-Lineage;
  Transformations- und Calculator-Version; Qualitätsdimensionen;
  Regelkatalog; Coverage, Freshness und Confidence; Quality Gates;
  Lineage-Abfrage für Data-Product-Versionen.
- **Nicht enthalten:** Kauf eines Governance-Produkts ohne Bedarfsanalyse;
  Blockieren aller Daten mit Warnungen; Speicherung unnötiger
  personenbezogener Rohpayloads im Audit.
- **Abhängigkeiten:** PM-01; PM-04; bestehende Import- und Generation-IDs.
- **Akzeptanzkriterien:** Eine Data-Product-Version ist auf Inputs und
  Transformationsversionen zurückführbar; Qualitätsbewertung ist
  reproduzierbar; Regeln besitzen Owner und Version; Raw- und Curated-Werte
  werden unterscheidbar ausgewiesen.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

### EPIC PM-06 – Vollständiges Data Product Management

- **Ziel:** Die vorhandene Data Product Engine zu einem beherrschten
  Produktlebenszyklus weiterentwickeln.
- **Ausgangslage:** Definition, Instanz, genau ein Generierungs-Scope,
  Customer Assignment, Availability, Generation Run, Version und Values sind
  vorhanden. Zwei Generatoren und REST-/React-Flows sind implementiert.
- **Umfang:** Ownership; Input-/Output-Verträge; Schemas; Qualitäts- und
  Freshness-Ziele; Gültigkeit; Lineage; Zugriffspolitik;
  Veröffentlichungsstatus; Freigabe; Deprecation; Reproduzierbarkeit;
  Concurrency-/Retry-Regeln; Produktkatalog.
- **Nicht enthalten:** Neuerstellung der vorhandenen Engine; Gleichsetzung
  jedes Dashboards mit einem Data Product; Marketplace ohne Geschäftsmodell.
- **Abhängigkeiten:** PM-04 und PM-05; Organization-/Access-Entscheidung.
- **Akzeptanzkriterien:** Jeder veröffentlichte Vertrag besitzt Owner,
  Consumer, Schema und SLOs; alte Versionen bleiben nachvollziehbar;
  Deprecation ist kontrolliert; Generierung ist bei gleichen Inputs
  reproduzierbar.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 2

Dabei ist mindestens folgende Begriffstrennung verbindlich zu definieren:

| Begriff | Arbeitsdefinition |
|---|---|
| `CalculationResult` | internes Ergebnis eines fachlichen Rechenlaufs |
| `BenchmarkDataset` | kuratierte Vergleichsgrundgesamtheit oder deren Ergebnis |
| KPI | definierte, versionierte Kennzahl |
| Feature | wiederverwendbare Eingangsgröße für Analyse oder ML |
| Report | zeitpunkt-/periodenbezogene Darstellung definierter Ergebnisse |
| Dashboard | interaktive Experience über Daten und Data Products |
| Data Product | verantworteter, versionierter Konsumvertrag |

### EPIC PM-07 – Governance, Datenschutz und Tenant-Zielmodell

- **Ziel:** Verantwortlichkeiten und Schutzregeln über den gesamten
  Datenlebenszyklus etablieren.
- **Ausgangslage:** JWT und Customer-bezogene Autorisierung bilden die
  MVP-Sicherheitsgrenze. Echte Mandantentrennung, Consent, Retention und
  administrative Löschprozesse sind nicht vollständig modelliert.
- **Umfang:** Data Owner/Steward/Product Owner; Datenschutz und Einwilligung;
  Retention; Tenant-Isolation; rollenbasierter Zugriff; Zweckbindung;
  Löschung, Anonymisierung und Archivierung; Audit-Aufbewahrung;
  Zugriff auf Rohdaten und Data Products.
- **Nicht enthalten:** Austausch der notwendigen MVP-Authentifizierung;
  Speicherung personenbezogener Daten „auf Vorrat“; Hard Delete ohne
  fachliche und rechtliche Regeln.
- **Abhängigkeiten:** PM-02; rechtliche Bewertung; Betriebsmodell.
- **Akzeptanzkriterien:** Rollen und Entscheidungsrechte sind benannt;
  Datenklassen besitzen Retention-/Löschregeln; Tenant-Grenzen sind getestet;
  Data-Product-Zugriff folgt demselben organisatorischen Scope.
- **Priorität:** P1
- **Empfohlener Zeithorizont:** Post-MVP 1

### EPIC PM-08 – Plattformbetrieb und Observability

- **Ziel:** ENSET nach dem MVP zuverlässig, messbar und kostenbewusst betreiben.
- **Ausgangslage:** API, Web, Worker-Grundlage, PostgreSQL und dateibasierte
  Raw Zone existieren. Produktiver Workerbetrieb, Scheduling,
  End-to-End-Telemetrie und ein vollständiges Storage-Betriebsmodell fehlen.
- **Umfang:** strukturierte Logs und Correlation IDs; Metriken und Traces;
  Import-/Generation-Latenz; Queue-/Job-Beobachtung; Wiederanlauf;
  Backup/Restore; Storage Retention; SLOs; Kapazitäts- und Kostenkontrolle;
  Betriebsverantwortung.
- **Nicht enthalten:** Kubernetes als Selbstzweck; Messaging ohne konkreten
  asynchronen Use Case; Ersatz produktionsnotwendiger MVP-Basistests.
- **Abhängigkeiten:** stabiler MVP; Mengengerüst und Betriebsanforderungen.
- **Akzeptanzkriterien:** kritische Flows sind beobachtbar; Runbooks und
  Verantwortliche existieren; Backup/Restore ist nachgewiesen; Kosten- und
  Kapazitätsgrenzen sind messbar.
- **Priorität:** P2
- **Empfohlener Zeithorizont:** Post-MVP 2

### EPIC PM-09 – Erweiterte Gebäude-, Anlagen- und Kontextdomänen

- **Ziel:** Das kanonische Modell entlang bestätigter Business Cases
  kontrolliert erweitern.
- **Ausgangslage:** Building, EnergySystem, Meter, Dokumente, Geography und
  vorbereitete Energy-Community-Modelle existieren. Raumstruktur, Sensorik,
  Wetter, Tarife und spezifische Anlagen sind nicht durchgängig implementiert.
- **Umfang:** bedarfsabhängige Modellierung von Floor, Room, Zone, PV-System,
  Heating System, Storage, Charging Station, Sensor, WeatherStation,
  WeatherObservation, Tariff, MarketData, EmissionFactor und weiteren Assets.
- **Nicht enthalten:** pauschale Umsetzung der gesamten Liste; eine Entity pro
  Produktidee; regulatorische Details ohne freigegebenen Anwendungsfall.
- **Abhängigkeiten:** PM-01 und PM-03; priorisierter Business Case;
  Identitäts- und Historisierungsregeln.
- **Akzeptanzkriterien:** Für jede Erweiterung existieren Bounded Context,
  Aggregate, Identität, Zeitbezug, Quelle, Owner und konsumierender Use Case;
  nicht benötigte Konzepte werden nicht implementiert.
- **Priorität:** P2
- **Empfohlener Zeithorizont:** Post-MVP 2

### EPIC PM-10 – Energy Community und Participant Vertical Slice

- **Ziel:** Energiegemeinschaften, Teilnehmer und Messpunkte als
  nachvollziehbaren fachlichen Kontext nutzbar machen.
- **Ausgangslage:** Grundmodelle für Energy Communities sind vorbereitet,
  bilden aber keinen vollständigen End-to-End-Use-Case. MeteringPoint und
  Participant benötigen noch klare Identitäts- und Assignment-Regeln.
- **Umfang:** Community, Participant, Mitgliedschaft, Rollen,
  MeteringPoint-Zuordnung, Gültigkeit, externe IDs, Freigaben und
  aggregierbare Scopes.
- **Nicht enthalten:** Marktkommunikation, Abrechnung oder regulatorische
  Vollautomatisierung ohne gesonderten Scope.
- **Abhängigkeiten:** PM-02, PM-03, PM-07; konkreter Community-Use-Case.
- **Akzeptanzkriterien:** Kein verwaister MeteringPoint; historische
  Mitgliedschaften sind nachvollziehbar; Aggregationen respektieren
  Gültigkeit und Berechtigung.
- **Priorität:** P2
- **Empfohlener Zeithorizont:** Post-MVP 2

### EPIC PM-11 – Externe Produkt-, Plattform- und Datenraum-Integration

- **Ziel:** Versionierte ENSET-Ergebnisse kontrolliert für externe Systeme
  bereitstellen.
- **Ausgangslage:** Versionierte REST-Endpunkte und Data-Product-DTOs
  bestehen. Vollständige externe Produktverträge, standardisierte Exporte,
  Marketplace- und Datenraum-Flows fehlen.
- **Umfang:** externe Data-Product-API; Exportprofile; Subscription/Webhook
  nach Bedarf; Vertragsversionierung; Consumer-Onboarding; Datenraum- und
  Plattformadapter; Publication Workflow.
- **Nicht enthalten:** direkter Zugriff externer Consumer auf interne Tabellen;
  Marketplace-Veröffentlichung ohne Governance; formatspezifische Logik im
  kanonischen Domainmodell.
- **Abhängigkeiten:** PM-06, PM-07 und PM-08.
- **Akzeptanzkriterien:** Externe Verträge sind versioniert; Consumer und
  Berechtigungen sind bekannt; Breaking Changes und Deprecation sind geregelt;
  Exporte sind reproduzierbar.
- **Priorität:** P2
- **Empfohlener Zeithorizont:** Post-MVP 2

### EPIC PM-12 – KI, Prognosen, Digital Twin und Knowledge Layer

- **Ziel:** Strategische Analysefähigkeiten nur auf belastbaren kanonischen,
  semantischen und qualitätsbewerteten Daten aufbauen.
- **Ausgangslage:** Generatoren und Analytics-Grundmodelle sind vorhanden;
  dedizierte Feature-, ML-, Digital-Twin- oder Knowledge-Graph-Plattformen
  bestehen nicht.
- **Umfang:** Forecast- und Anomalie-Features; Modellversionierung;
  Trainings-/Inferenz-Lineage; Optimierungsservices; Prüfung eines Digital
  Twin; Ontologie/Knowledge Graph nur bei nachgewiesenem Beziehungs- und
  Abfragebedarf.
- **Nicht enthalten:** KI als Ersatz für Identitätsauflösung oder
  Datenqualität; automatisch bindende Entscheidungen ohne Audit;
  Knowledge Graph als vorsorgliche Plattformkomponente.
- **Abhängigkeiten:** PM-04 bis PM-08; konkrete Business Cases und
  Qualitätsgrenzen.
- **Akzeptanzkriterien:** Use Case und Nutzen sind messbar; Features und
  Modelle sind versioniert; Ergebnisse besitzen Confidence und Lineage;
  Human Oversight ist für relevante Entscheidungen definiert.
- **Priorität:** P3
- **Empfohlener Zeithorizont:** Long-Term

## 5. Architekturentscheidungen mit Klärungsbedarf

Die folgenden Punkte sind noch keine bestätigten Implementierungsaufgaben:

1. **Organization-Kardinalität:** Gehört jedes Building genau einer primären
   Organization, oder ist ausschließlich ein historisiertes Rollenmodell
   zulässig? Empfehlung: verbindlicher primärer Scope plus zusätzliche Rollen.
2. **Customer-Zukunft:** Bleibt Customer ein kommerzieller Vertragspartner
   neben Organization oder wird er vollständig migriert?
3. **Tenant-Grenze:** Ist Tenant identisch mit Organization, einer
   Organization-Gruppe oder einem technischen Betreiberkontext?
4. **MeteringPoint-Identität:** Welche externen IDs und regulatorischen
   Gültigkeitsregeln gelten je Markt und Quelle?
5. **Meterwechsel:** Werden Readings primär an Meter und semantisch über ein
   zeitliches Assignment an MeteringPoint gebunden?
6. **Historisierung:** Welche Aggregate benötigen fachliche Versionstabellen,
   Revisionen oder lediglich Audit? Eine pauschale bitemporale Lösung ist zu
   vermeiden.
7. **Semantic Layer:** Persistierte semantische Facts oder berechnete Views?
   Entscheidung anhand Volumen, Reproduzierbarkeit und Freshness.
8. **Data-Product-Grenze:** Wann wird ein internes Ergebnis veröffentlicht und
   welcher Freigabeschritt ist erforderlich?
9. **CRM-Führerschaft:** Welche Attribute führt Twenty CRM, welche ENSET und
   wie werden Konflikte gelöst?
10. **Knowledge Layer:** Erst nachweisen, dass Ontologie-/Graphabfragen einen
    Vorteil gegenüber kanonischen Relationen und Semantic Models besitzen.
11. **Data Mesh:** Als Ownership- und Governance-Modell verwenden; eine
    organisatorische Dezentralisierung oder Microservice-Aufteilung ist eine
    separate Entscheidung.
12. **Physische Lakehouse-Zonen:** Getrennte Stores nur nach Mengen-,
    Sicherheits- oder Betriebsnachweis einführen.

## 6. Technische Schulden nach dem MVP

Folgende Punkte sind nach MVP-Abschluss neu zu bewerten. Sie dürfen nicht mit
noch offenen MVP-Blockern vermischt werden:

- historische Architektur- und Roadmap-Dokumente konsolidieren;
- veraltete Aussagen zu `DatabaseImportWriter`, Reportpersistenz,
  Authentifizierung und Data Products markieren;
- überlappende Excel-Reader-/Writer-Abstraktionen fachlich benennen;
- ungenutzte Data-Product-Platzhalter entfernen oder vertraglich aktivieren;
- Namespace- und Benennungsinkonsistenzen bereinigen;
- Concurrency-/Retry-Strategie für Data-Product-Versionen ergänzen;
- Worker-Betriebsmodell, Scheduling und idempotente Jobs entscheiden;
- TimescaleDB beziehungsweise Partitionierung erst nach Lasttests evaluieren;
- Marketplace-, Mobility-, Subscription-, Aggregation- und Analytics-Gerüste
  nur bei bestätigtem Scope behalten;
- Schema- und API-Vertragsversionierung vereinheitlichen;
- Migrationen aus dem API-Startup in ein kontrolliertes Deploymentverfahren
  überführen;
- Testpyramide um Contract-, Komponenten-, Browser- und Lasttests erweitern.

Gefundene Dokumentationswidersprüche:

- `docs/03_Data_Lake_House.md`, `docs/04_Import.md`,
  `ARCHITECTURE_BASELINE_V1_0_RC.md` und das RC-Review beschreiben den
  relationalen `DatabaseImportWriter` teilweise noch als nicht implementiert.
  Der aktuelle Code besitzt inzwischen einen relationalen Writer für
  Customer, Building, Meter, rohe und kuratierte MeterReadings.
- ältere Dokumente bezeichnen Data Products als vollständig offen. Der aktuelle
  Stand enthält dagegen Definitionen, Instanzen, Scopes, Assignments,
  Generation Runs, Versionen, Values, zwei Generatoren, REST und React.
- ältere Reviews nennen Reports als relational oder nicht relational, während
  die aktive API-Konfiguration derzeit den dateibasierten
  `JsonImportReportRepository` verwendet. Eine verbindliche
  Produktionspersistenz ist zu dokumentieren.
- Testanzahlen und Reifegradwerte in Reviews sind Momentaufnahmen und dürfen
  nicht als aktueller Qualitätsnachweis verwendet werden.
- `Customer` wird teilweise als Organisation, Mandant, Berechtigungsscope und
  Vertragspartner verwendet; die LEB-Dokumentation benennt bereits
  `Organization` als langfristiges Ziel.

## 7. Fachliche Erweiterungen

Fachliche Erweiterungen werden nicht anhand vorhandener Entity-Gerüste,
sondern anhand eines freigegebenen Use Cases priorisiert.

| Kandidat | Plausibler Nutzen | Offene Modellierungsentscheidung | Einordnung |
|---|---|---|---|
| Floor/Room/Zone | Flächen-, Anlagen- und Sensorzuordnung | Hierarchie, zeitliche Gültigkeit | PM-09 |
| PV-System | Erzeugung und Eigenverbrauch | Asset oder EnergySystem-Subtype | PM-09 |
| Heating System | Wärmeversorgung und Effizienz | Systemgrenze, Energieträger | PM-09 |
| Storage | Energiefluss und Optimierung | Kapazität, Ladezustand, Metering | PM-09 |
| Charging Station | Mobilität und Lastmanagement | Asset, EVSE, Messpunkt | PM-09 |
| Sensor | zusätzliche Beobachtungen | Sensor versus Meter, Kalibrierung | PM-09 |
| WeatherStation/-Observation | Wetterbereinigung | externe Quelle, räumliche Zuordnung | PM-04/09 |
| Tariff/MarketData | Kosten und Optimierung | Vertrag versus Marktreferenz | PM-04/09 |
| EmissionFactor | CO2e-Kennzahlen | Region, Zeitraum, Methodikversion | PM-04 |
| EnergyCommunity/Participant | Gemeinschaftsaggregation | Rollen, Identität, Consent | PM-10 |
| Renovation/Certificate | Sanierungsplanung/EPBD | Rechtsstand, Dokumente, Versionen | Long-Term |

Keines dieser Modelle wird allein aufgrund dieser Liste verpflichtend. Vor
Umsetzung müssen Identität, Owner, fachlicher Lebenszyklus, Quelle,
Historisierung und mindestens ein konsumierender Use Case feststehen.

## 8. Empfohlene Reihenfolge nach MVP-Abschluss

### Post-MVP 1

1. PM-01 – Zielarchitektur und Begriffe konsolidieren.
2. PM-02 – Organization-/Customer-/CRM-Grenzen entscheiden.
3. PM-03 – historisierte Assignments und MeteringPoint-Invarianten.
4. PM-07 – Governance-, Tenant- und Datenschutzrahmen.
5. PM-04 – zentrale semantische und analytische Modelle.
6. PM-05 – durchgängige Lineage und Quality.

### Post-MVP 2

1. PM-06 – Data Product Management vervollständigen.
2. PM-08 – Plattformbetrieb und Observability ausbauen.
3. PM-09 – priorisierte Asset-/Kontextdomänen als Vertical Slices.
4. PM-10 – Energy Community/Participant bei bestätigtem Bedarf.
5. PM-11 – externe Produkt- und Datenraum-Verträge.

### Long-Term

1. PM-12 – KI-, Forecast-, Digital-Twin- und Knowledge-Use-Cases.
2. Data-Mesh- und Bounded-Context-Strukturen organisatorisch ausbauen, wenn
   mehrere verantwortliche Domänenteams tatsächlich existieren.
3. Modularisierung oder Aufteilung von Deployables nur entlang gemessener
   Änderungs-, Skalierungs- oder Betriebsgrenzen.

## 9. Scope-Gate für neue Anforderungen

Verbindliche Entscheidungsregel:

> Eine neue Anforderung darf nur in den MVP aufgenommen werden, wenn sie
> unmittelbar erforderlich ist, um vorhandene Daten zu importieren,
> anzuzeigen, zu reporten oder als erstes nutzbares Data Product
> bereitzustellen.

Alle anderen Anforderungen werden im Post-MVP-Backlog erfasst.

Für jede neue Anforderung sind diese Fragen zu beantworten:

1. Verhindert ihr Fehlen einen bereits freigegebenen MVP-End-to-End-Flow?
2. Wird sie für vorhandene Daten und einen bestehenden MVP-Consumer benötigt?
3. Kann der MVP ohne neue Domäne, neue Plattform oder neue generische Engine
   fachlich korrekt abgeschlossen werden?
4. Ist sie Fehlerbehebung beziehungsweise notwendige Produktionsbasis oder
   eine Erweiterung des Zielbilds?
5. Existieren Akzeptanzkriterien, Owner und ein konkreter MVP-Nachweis?

Entscheidung:

- **MVP:** Nur wenn Frage 1 oder 2 eindeutig mit Ja beantwortet ist und die
  kleinste sichere Lösung keine Post-MVP-Architektur vorwegnimmt.
- **Post-MVP:** Bei neuem Kundensegment, neuer Domäne, generischer Plattform,
  umfassender Governance, CRM, Data Mesh, Knowledge Graph, Digital Twin,
  zusätzlicher Analysefamilie oder externem Produktvertrag.
- **Verwerfen/Beobachten:** Wenn weder konkreter Consumer noch messbarer Nutzen
  und Owner vorhanden sind.

Eine Post-MVP-Idee darf nicht über einen vermeintlichen „kleinen technischen
Vorbau“ in den MVP gelangen. Ausnahmen benötigen eine dokumentierte
Scope-Entscheidung mit Auswirkung, Aufwand und entfallenem MVP-Inhalt.
