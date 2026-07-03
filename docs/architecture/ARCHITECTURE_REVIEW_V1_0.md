
# Architecture Compliance Roadmap (Version 1.0)

## Ziel

Vor der weiteren fachlichen Entwicklung wird die bestehende Implementierung auf die verbindliche **ENSET Architecture Baseline v1.0** angeglichen.

Der Code bildet bereits eine sehr gute Grundlage. Ein Neuaufbau ist **nicht erforderlich**. Stattdessen werden gezielt die noch offenen Architekturpunkte geschlossen, bevor neue Features entwickelt werden.

---

# Ergebnis des Architecture Reviews

## Positiv

- Clean Architecture ist vorhanden.
- Trennung von Domain, Application und Infrastructure ist korrekt.
- MeterReading ist als Zeitreihenobjekt korrekt modelliert.
- Composite Key (`MeterId + Timestamp`) ist umgesetzt.
- Fachliche (`MeterNumber`) und technische (`MeterId`) Identitäten sind sauber getrennt.
- Domain enthält keine EF-, EMS- oder Data-Space-Abhängigkeiten.

## Architekturabweichungen

Es bestehen derzeit keine grundlegenden Architekturfehler, sondern hauptsächlich unvollständige Implementierungen der Baseline v1.0.

Insbesondere fehlen:

- Application-gesteuerter Importworkflow
- vollständiger DuplicationCheck-Workflow
- Data Product Layer
- produktionsreife Importpipeline
- externe Konfiguration
- Infrastruktur für Betrieb und Deployment

---

# Phase A – Architekturabgleich (P0)

Diese Arbeiten müssen vor jeder weiteren fachlichen Entwicklung abgeschlossen werden.

## AP A.1 Import-Workflow absichern

### Ziel

Ein Import darf nach einem `Abort` niemals Daten verändern.

### Aufgaben

- Write-Gate implementieren
- keine Excel-Aktualisierung nach Abort
- keine Datenbankaktualisierung nach Abort
- automatisierte Tests

Status

- [ ] Offen

---

## AP A.2 Application Import Use Case

### Ziel

Die gesamte Importorchestrierung erfolgt ausschließlich in `Enset.Application`.

### Aufgaben

- ImportCoordinator / ImportUseCase einführen
- Worker ruft ausschließlich diesen Use Case auf
- Reader und Writer bleiben Infrastructure-Adapter

Status

- [ ] Offen

---

## AP A.3 DuplicationCheck

### Ziel

Dubletten werden vollständig als fachlicher Workflow modelliert.

### Aufgaben

- ImportIssue
- DuplicateCandidate
- ResolutionAction
- Benutzerentscheidung
- Audit Trail

Automatische Dublettenzusammenführung ist nicht zulässig.

Status

- [ ] Offen

---

## AP A.4 Resolution-gesteuerter Writer

### Ziel

Schreibzugriffe erfolgen ausschließlich nach Benutzerfreigabe.

### Aufgaben

- Writer akzeptiert nur freigegebene Resolution
- keine automatischen Änderungen
- keine impliziten Merges

Status

- [ ] Offen

---

## AP A.5 Data Product Layer

### Ziel

Business Modules dürfen ausschließlich Data Products konsumieren.

### Aufgaben

- DataProduct
- IDataProductPublisher
- IDataProductQueryPort

Status

- [ ] Offen

---

## AP A.6 Konfiguration

### Ziel

Produktionsreife Konfiguration.

### Aufgaben

- Connection Strings auslagern
- Credentials entfernen
- Dateipfade konfigurieren
- appsettings.json
- Environment Variables

Status

- [ ] Offen

---

## AP A.7 CSV-Validierung

### Ziel

Ungültige Daten dürfen niemals stillschweigend verarbeitet werden.

### Aufgaben

- ImportIssue erzeugen
- keine impliziten Nullwerte
- keine automatischen Korrekturen
- keine stillschweigenden Skips

Status

- [ ] Offen

---

# Phase B – Plattformstabilisierung (P1)

Nach Abschluss des Architekturabgleichs.

- [ ] Raw Zone implementieren
- [ ] Curated Zone implementieren
- [ ] Persistenten ImportJob einführen
- [ ] DataSource persistieren
- [ ] TimescaleDB-Strategie finalisieren
- [ ] Unit Tests ergänzen
- [ ] Worker auf Generic Host umstellen
- [ ] Docker & Docker Compose
- [ ] CI/CD einrichten
- [ ] Data Product Pipeline implementieren

---

# Phase C – Fachliche Weiterentwicklung

Nach erfolgreichem Architekturabgleich.

## Geplante Themen

- Weitere Importer
- Energy Management
- Benchmarking
- Calculation Service
- REST API
- Dashboard
- WinUI
- Web Platform
- Business Modules
- Data Space
- Marketplace

---

# Definition of Done

Der Architekturabgleich gilt als abgeschlossen, wenn:

- alle P0-Aufgaben abgeschlossen sind
- der Import vollständig Application-gesteuert ist
- keine Schreibzugriffe ohne Benutzerfreigabe möglich sind
- Business Modules ausschließlich Data Products konsumieren
- die Codebasis der ENSET Architecture Baseline v1.0 entspricht

Danach bildet die aktuelle Implementierung die verbindliche Referenz für alle weiteren Entwicklungsarbeiten.
# ENSET Data Lake House MVP – Architecture Review v1.0

**Prüfdatum:** 2026-07-02  
**Prüfgegenstand:** aktueller Stand des gesamten Repository  
**Verbindliche Referenz:** ENSET Architecture Baseline v1.0  
**Gesamtentscheidung:** **Code benötigt Anpassungen vor Version 1.0.**

## 1. Executive Summary

Der vorhandene Code bildet einen kompilierbaren technischen Prototyp mit einer grundsätzlich korrekten Abhängigkeitsrichtung zwischen `Domain`, `Application` und `Infrastructure`. Das zentrale Zeitreihenmodell ist in wichtigen Punkten richtig umgesetzt: `MeterReading` erbt nicht von `BaseEntity`, der Schlüssel besteht aus `MeterId + Timestamp`, und `Meter.MeterNumber` ist als eindeutige fachliche Identität von der technischen GUID getrennt.

Die ENSET Architecture Baseline v1.0 ist dennoch noch nicht erfüllt. Der Hauptgrund ist nicht eine falsche Grundstruktur, sondern das Fehlen verbindlicher Anwendungsgrenzen und vollständiger Workflows:

- Der Import-Worker verwendet konkrete Infrastructure-Klassen direkt und enthält Test-/Dateizugriffs-/Writerlogik statt ausschließlich einen Application Use Case aufzurufen.
- Bei Validierungsfehlern entscheidet der Code zwar `Abort`, führt Excel-Änderungen anschließend aber ausdrücklich trotzdem aus.
- `ImportIssue`, `DuplicateCandidate` und `ResolutionAction` existieren nicht als getrennte Modelle. Dubletten werden nur als Fehlertext gemeldet; eine explizite Benutzerfreigabe ist nicht modelliert.
- Raw Zone, Curated Zone und Data Products sind nur in Dokumentationsdiagrammen vorhanden. Es gibt keine implementierten Data-Product-Verträge, keinen Erzeugungsprozess und keine technisch durchsetzbare Konsumgrenze für Business Modules.
- Die Persistenz ist PostgreSQL-kompatibel, provisioniert jedoch keinen nachweisbaren TimescaleDB-Hypertable.
- Docker/Compose, externe Konfiguration, strukturiertes Logging, automatisierte Tests und CI fehlen.

Es ist kein großer Neuaufbau nötig. Die bestehende Layer-Aufteilung kann beibehalten werden. Vor fachlicher Weiterarbeit oder Integration von Business Modules ist aber ein kleiner, gezielter Architekturabgleich erforderlich: Application-seitige Import-Orchestrierung, expliziter Review-/Resolution-Workflow, sichere Konfiguration und Data-Product-Port. Die derzeitige ausführbare Worker-Testlogik darf nicht als produktiver Importpfad verwendet werden.

## 2. Prüfumfang und Nachweise

Geprüft wurden sämtliche versionierten Projekt-, C#-, Migrations- und Dokumentationsdateien sowie das Vorhandensein von Betriebs- und Testartefakten. Binärdateien unter `Externe Daten/` und `Exportierte Daten/` wurden als Ein-/Ausgabedaten inventarisiert, nicht fachlich ausgewertet.

Build-Nachweis mit installiertem .NET SDK 10.0.202:

```text
dotnet build src/Enset.Domain/Enset.Domain.csproj --no-restore          erfolgreich, 0 Warnungen
dotnet build src/Enset.Application/Enset.Application.csproj --no-restore     erfolgreich, 1 Warnung
dotnet build src/Enset.Infrastructure/Enset.Infrastructure.csproj --no-restore erfolgreich, 0 Warnungen
dotnet build src/Enset.Worker.Import/Enset.Worker.Import.csproj --no-restore  erfolgreich, 0 Warnungen
```

Die Application-Warnung `CS8618` betrifft `RawDataObject.FilePath`. Es existieren keine Testprojekte; deshalb war kein automatisierter Verhaltensnachweis möglich. Datenbankmigrationen wurden statisch geprüft, aber mangels bereitgestellter Datenbank-/Compose-Umgebung nicht gegen eine reale Instanz ausgeführt.

## 3. Ist-Architektur

### 3.1 Repository- und Projektstruktur

Es gibt keine Solution-Datei. Vorhanden sind vier Projekte:

| Projekt | Typ und Verantwortung | Referenzen | Bewertung |
|---|---|---|---|
| `src/Enset.Domain` | Klassenbibliothek; Entities, Enums, fachliches Grundmodell | keine Projekt- oder Package-Referenz | Schichtengrenze korrekt |
| `src/Enset.Application` | Klassenbibliothek; Import-DTOs, Ports, Validierung, Entscheidungen, Reports | `Enset.Domain` | Referenzrichtung korrekt, Use-Case-Orchestrierung unvollständig |
| `src/Enset.Infrastructure` | Klassenbibliothek; EF Core, Npgsql, ClosedXML, Reader, Writer, Lookup | `Enset.Domain`, `Enset.Application` | Referenzrichtung korrekt |
| `src/Enset.Worker.Import` | Console Executable; derzeit manueller Test-/Democode | `Enset.Infrastructure`, `Enset.Application`, `Enset.Domain` | verletzt die geforderte Worker-Grenze |

Nicht vorhanden sind API-, UI- und Testprojekte. Ein produktiver Hosted Worker ist ebenfalls nicht vorhanden; `Enset.Worker.Import` ist ein direkt ausgeführtes Top-Level-Programm mit hart codierten lokalen Pfaden.

Weitere relevante Verzeichnisse:

- `docs/`: Architektur-, Datenmodell-, Import- und Roadmap-Dokumente; `docs/Decisions/adr-001-architecture.md` ist leer.
- `Externe Daten/`: CSV-/XLSM-Eingabedateien und eine Windows-Verknüpfung.
- `Exportierte Daten/`: eine erzeugte XLSM-Datei.
- `src/Enset.Infrastructure/Migrations/`: vier EF-Core-Migrationen und Model Snapshot.

### 3.2 Namespaces und Ordner

| Schicht | Haupt-Namespaces/Ordner |
|---|---|
| Domain | `Common`, `Customers`, `Projects`, `Buildings`, `Energy`, `Documents`, `Analytics`, `Geography`, `Data` |
| Application | `Exports.Abstractions`, `Imports.Abstractions`, `AutoFix`, `Decisions`, `DTOs`, `Enums`, `Models`, `Normalizer`, `Reports`, `Services`, `Validation` |
| Infrastructure | `Exports.Excel`, `Imports.Readers`, `Imports.Excel`, `Imports.Factory`, `Imports.Mapping`, `Imports.Services`, `Persistence`, `Migrations` |
| Worker | Top-Level-Programm ohne eigenen Namespace |

Mehrere Domain-Enums besitzen keinen Namespace (`KPIType`, `ScopeLevel`, `CustomerType`, `ProjectStatus`, Building-/Energy-/Document-Enums, `DataSourceType`). Das kompiliert, erzeugt aber einen globalen Typnamensraum und widerspricht der sonstigen Paketstruktur. Zusätzlich weichen Ordner und Namespace bei CSV Reader und Factory voneinander ab (`Csv` → `Imports.Readers`, `Factories` → `Imports.Factory`). Dies ist kein Baseline-Bruch, erschwert aber Navigation und künftige Architekturtests.

Leere Platzhalterdateien existieren unter anderem für Customer-/Building-/Meter-Mapper, spezialisierte Excel-Reader, Validatoren, Normalizer, AutoFix Engine, `ImportDecision`, `CustomerImportDto` und `IExcelValidator`. Sie sind keine implementierten Komponenten.

### 3.3 Tatsächlicher Abhängigkeitsfluss

```text
Enset.Domain
    ↑
Enset.Application
    ↑          ↖
Enset.Infrastructure ← Enset.Worker.Import
                         ↘ direkte konkrete Reader/Writer/ClosedXML-Nutzung
```

Die Bibliotheksreferenzen folgen Clean Architecture. Der Composition Root/Worker tut dies fachlich jedoch nicht: Er instanziiert `CsvMeterReadingReader`, `ExcelWorkbookReader` und `ExcelWorkbookWriter` direkt und orchestriert Validierung, Entscheidung und Dateischreibvorgänge selbst. `IImportService` und dessen Implementierung liegen beide in Infrastructure; damit besitzt Application keinen vollständigen Import-Use-Case, über den Worker/API/UI arbeiten könnten.

## 4. Clean-Architecture-Konformität

| Anforderung | Status | Befund |
|---|---|---|
| Domain ohne Application/Infrastructure/EF Core/konkrete I/O-Typen | **Erfüllt** | Domain-Projekt hat keine Referenzen oder Packages und verwendet nur eigene Typen/BCL. |
| Application darf Domain, nicht Infrastructure referenzieren | **Erfüllt** | Einzige Projekt-Referenz ist Domain. |
| Infrastructure darf Domain und Application referenzieren | **Erfüllt** | Projektgraph entspricht der Regel. |
| Worker/API/UI arbeiten nur über Application/Services | **Nicht erfüllt** | Worker referenziert alle Schichten, erzeugt konkrete Reader/Writer und steuert den Prozess selbst. |
| Ports innen, Adapter außen | **Teilweise** | Reader-/Writer-/Lookup-Ports liegen in Application; der zentrale `IImportService` liegt fälschlich neben seiner Implementierung in Infrastructure. |
| Mapping ohne Persistenz-Seiteneffekt | **Derzeit faktisch offen** | `MeterReadingMapper` ist vollständig auskommentiert. Der auskommentierte Entwurf ruft während des Mappings `SaveChangesAsync` auf und wäre bei Aktivierung abzulehnen. |

Positive Feststellung: Im Domain-Modell findet sich keine EMS-, Data-Space- oder externsystemspezifische Logik. Auch konkrete Dateiformate und EF Core bleiben außerhalb der Domain.

## 5. Konformität mit den sechs Baseline-Prinzipien

| # | Prinzip | Status | Bewertung des aktuellen Codes |
|---:|---|---|---|
| 1 | Business Modules konsumieren ausschließlich Data Products | **Nicht nachweisbar / nicht implementiert** | Business Modules fehlen. Ebenso fehlen Data-Product-Verträge und ein Application-Port, der direkten Storage-Zugriff technisch verhindert. Die Dokumentation beschreibt das Ziel korrekt. |
| 2 | Data Lake House ist Single Source of Truth für historische Energie-/Analysedaten | **Teilweise** | `MeterReading`, `CalculationResult` und `BenchmarkDataset` sind persistierbar. Der produktive Import, Provenance, Raw-Aufbewahrung und Data-Product-Publikation fehlen. Excel wird durch den Worker als beschreibbares Arbeitsartefakt behandelt; ohne Führungsregel drohen konkurrierende Wahrheiten. |
| 3 | Data Platform ist Anwendungsplattform, nicht Data Lake House | **Konzeptionell erfüllt** | Im Code ist keine Data-Platform-UI oder Business-Module-Logik eingebaut. Die Dokumentation trennt UI/API und Lake-House-Speicher grundsätzlich. |
| 4 | Energy Management entscheidet, erzeugt aber keine Data Products | **Konzeptionell erfüllt** | Kein EMS-Code vorhanden; die Dokumentation zeigt EMS nur als Konsument des Data Product Service. |
| 5 | Integration Layer ist einzige standardisierte externe Schnittstelle | **Teilweise** | Reader/Writer sind Infrastructure-Adapter hinter Application-Ports. Der Worker umgeht diese Grenze durch direkte Nutzung und fest verdrahtete Dateipfade; ein standardisierter Integration Entry Point fehlt. |
| 6 | Data Space dient Austausch, nicht Verarbeitung | **Konzeptionell erfüllt** | Kein Data-Space-Code vorhanden. In `docs/01_Architecture.md` hängt der Connector am Exportpfad; Verarbeitung ist dort nicht eingezeichnet. |

## 6. Data-Lake-House-Konformität

### 6.1 Zonenmodell

Raw, Curated/Silver und Data Products/Gold sind in `docs/01_Architecture.md`, `docs/03_Data_Lake_House.md` und `docs/04_Import.md` beschrieben, im Code jedoch nicht als getrennte Ports, Speicherorte, Schemas oder Pipelinestufen umgesetzt.

- **Raw Zone:** `RawDataObject` enthält lediglich `Id`, `Type` und `FilePath`; es gibt keinen Raw Store, keine unveränderliche Ablage, Prüfsumme, Herkunft oder Retention.
- **Curated Zone:** `DataQuality` und persistierbare Entities existieren, aber kein Curated Store bzw. Freigabe-/Qualitäts-Gate.
- **Data Products:** Keine Klasse, kein Vertrag, keine Version, kein Schema, keine Metadaten und kein Publisher/Repository vorhanden.

Es gibt aktuell keine Businesslogik, die direkt Raw Data liest. Deshalb liegt noch kein konkreter Business-Module-Verstoß vor. Der Worker liest Rohdateien direkt, was für einen Ingestion-Adapter legitim wäre, aber nur hinter einem Application Use Case und einem Raw-Persistenzschritt erfolgen sollte.

### 6.2 Berechnung, Benchmark, Validierung und Produktbildung

`CalculationResult` und `BenchmarkDataset` modellieren speicherbare Analyseergebnisse. Berechnungs-, Benchmark-, Qualitäts-, Aggregations-, Anonymisierungs- und Data-Product-Services sind ausschließlich geplant. Damit ist die vorgeschriebene Reihenfolge

```text
Raw → Validierung/Normalisierung → Curated → Berechnung/Benchmark/QA → Data Product
```

nicht verletzt, aber auch nicht implementiert oder durch Tests abgesichert. `CalculationResult` darf nicht mit einem Data Product gleichgesetzt werden; ihm fehlen Vertrag, Version, Qualitätsstatus, Provenance und Publikationsmetadaten.

## 7. Import- und DuplicationCheck-Workflow

### 7.1 Vorhandene Bausteine

- Reader: `ExcelWorkbookReader`, `CsvMeterReadingReader`.
- DTO/Zeilenmodelle: `CustomerExcelRow`, `BuildingExcelRow`, `MeterImportDto`, `MeterReadingImportDto`.
- Validierung: `ExcelImportValidator` für IDs und Customer-Building-Referenzen.
- Reports/Entscheidung: `ImportReport`, `ImportDecisionEngine` mit `Continue` oder `Abort`.
- Writer: `ExcelWorkbookWriter` für Kopie und gezielte Feld-/ID-/Notizänderungen.
- Lookup: `IMeterLookupService` / `MeterLookupService`.
- Orchestrierung: `ImportService` ist nur ein Stub; der Worker übernimmt derzeit den Ablauf.

### 7.2 Kritische Ablaufbefunde

1. **Keine typisierte Problemtrennung.** `ImportIssue`, `DuplicateCandidate` und `ResolutionAction` existieren nicht. Fehler und Dubletten sind unstrukturierte Strings in `ImportReport.Errors`.
2. **Keine fachliche Dublettenentscheidung.** Der Validator erkennt doppelte interne Customer-/Building-IDs innerhalb der Excel-Datei, aber keine Kandidaten über normalisierte fachliche Merkmale oder gegen den Bestand. Es gibt kein Merge-Modell – positiv ist daher, dass derzeit auch kein automatischer Merge implementiert ist.
3. **Keine Benutzerfreigabe.** `ImportDecisionEngine` entscheidet rein technisch. Benutzeridentität, gewählte Resolution, Zeitstempel, Audit Trail und unveränderliche Entscheidung fehlen.
4. **Expliziter Gate-Verstoß im Worker.** Bei `Abort` protokolliert `Program.cs`, dass trotzdem fortgefahren wird, und führt anschließend Excel-Updates aus. Fehlende IDs können ohne Benutzerentscheidung generiert und geschrieben werden.
5. **Writer ist nicht an eine genehmigte Resolution gebunden.** Jede aufrufende Schicht kann Felder oder IDs direkt ändern. Das Interface akzeptiert keine bestätigte `ResolutionAction`.
6. **CSV-Fehler werden verschluckt.** Ungültige oder kurze Zeilen werden übersprungen; nicht parsebare Werte werden zu `0m`. `MeterNumber`, Einheit und Quality werden nicht aus der Datei gesetzt. So kann Datenverlust bzw. verfälschte Curated Data entstehen, ohne `ImportIssue` zu erzeugen.
7. **Kein atomarer DB-Write.** Der aktive `ImportService` validiert, mappt oder persistiert nicht. Transaktion, Idempotenz, Wiederanlauf und Statusübergänge sind nicht vorhanden.

Erforderlicher Zielablauf:

```text
Ingest + unveränderliche Raw-Kopie
  → Parse/Map in DTOs
  → ValidationIssues + DuplicateCandidates erzeugen
  → Import pausieren (AwaitingResolution)
  → Benutzer bestätigt ResolutionActions
  → Resolutionen validieren und auditieren
  → atomar Curated/DB schreiben
  → optional genehmigte Excel-Kopie aktualisieren
  → Berechnung/QA
  → versioniertes Data Product publizieren
```

Excel-/DB-Writer dürfen nur eine bestätigte Resolution bzw. einen freigegebenen Importplan ausführen. Originaldateien der Raw Zone bleiben unverändert.

## 8. Datenmodell

| Modell | Status | Befund |
|---|---|---|
| `Customer` | vorhanden | Basis-Entity mit Name/Typ und Projektnavigation; fachliche Import-ID ist nicht im Entity modelliert. |
| `Project` | vorhanden | Customer-FK, Name, Start, Status, Buildings/Documents. |
| `Building` | vorhanden | Project-/District-FK, Nutzung, Kategorie, Eigentum, Flächen und Energiesysteme/Meter. |
| `Meter` | vorhanden | Technische GUID über `BaseEntity`; `MeterNumber` als fachliche Identität; eindeutiger DB-Index. |
| `MeterReading` | **korrekt im Kern** | Keine `BaseEntity`; `MeterId`, `Timestamp`, Wert, Einheit, Qualität und Herkunfts-ID. Composite Key ist konfiguriert. |
| `EnergySystem` | vorhanden | Building-FK, Typ, Kapazität und Installationsjahr. |
| `Document` | vorhanden | Project-FK, Typ, Pfad, Uploadzeit. |
| `CalculationResult` | vorhanden | KPI, Scope, Wert/Einheit und Zeitraum; Scope ist nur polymorph über `ScopeLevel + ScopeId`, ohne relationale Integrität. |
| `BenchmarkDataset` | vorhanden | Scope/Region/Kategorie/Zeitraum, Durchschnitt und Stichprobengröße; Version/Quelle/Gültigkeit fehlen. |

Weitere Risiken:

- `MeterReading.CustomerId`, `BuildingId` und `SourceImportJobId` sind denormalisierte GUIDs ohne konfigurierte Foreign Keys. Sie können vom zugehörigen `Meter` oder gelöschten Stammdaten abweichen.
- `SourceImportJobId` verweist auf ein Modell, dessen Tabelle in der letzten Migration entfernt wurde; die Provenance ist damit nicht referenziell abgesichert.
- `ImportJob` liegt in Application, erbt von Domain-`BaseEntity` und navigiert auf `Project`, wird aber nicht persistiert. Prozesszustand und fachliches Domain-Modell sind dadurch unscharf getrennt.
- Datenbankseitige Längen-, Check- und Präzisionsregeln fehlen weitgehend. Zeitstempel sollten als UTC-Invariante validiert werden.
- Die fachliche Eindeutigkeit von `MeterNumber` ist sauber als Unique Index umgesetzt; Normalisierung/Case-Regel für eingehende Nummern ist noch nicht definiert.

## 9. Persistenz, EF Core und Migrationen

### 9.1 Positive Befunde

- `EnsetDbContext` liegt ausschließlich in Infrastructure.
- DbSets existieren für Customer, Project, Building, EnergySystem, Meter, MeterReading, Document, CalculationResult und BenchmarkDataset.
- Geografie-Entities werden über Navigationen ins EF-Modell aufgenommen, auch wenn explizite DbSets fehlen.
- Der aktuelle Snapshot enthält den Composite Primary Key `MeterId + Timestamp`, den Timestamp-Index und den eindeutigen `MeterNumber`-Index.
- Migrationen sind vorhanden und spiegeln die Entwicklung vom GUID-Schlüssel zum Zeitreihenschlüssel wider.

### 9.2 Abweichungen und Risiken

- **Kein Timescale-Hypertable:** `ConfigureMeterReadingTimescale` ändert nur Schlüssel/Spalten/Indizes. Weder `CREATE EXTENSION timescaledb` noch `create_hypertable(...)` ist enthalten. Die Benennung der Migration belegt daher keine Timescale-Provisionierung.
- **Import-Provenance entfernt:** `SyncModelAfterRefactor` löscht `ImportJobs` und `DataSources`, obwohl Klassen und Dokumentation sie weiterhin als Importbestandteil führen.
- **Unsichere Bestandsmigration:** Dieselbe Migration fügt `MeterNumber NOT NULL DEFAULT ''` hinzu und legt danach einen Unique Index an. Bei mehr als einem bestehenden Meter scheitert dies oder benötigt vorab ein Backfill.
- **Hardcoded Credentials:** `EnsetDbContextFactory` enthält Host, Datenbank, Benutzer und Passwort im Quellcode. Auch Entwicklungszugänge müssen aus Konfiguration/Secret-Mechanismus kommen.
- **Versionsdrift:** EF Core Runtime ist `10.0.4`, EF Design und Npgsql Provider sind `10.0.1`. Das ist derzeit buildbar, sollte aber zentral und kompatibel gepinnt werden.
- **Keine Laufzeitregistrierung:** Es gibt keinen Composition Root mit `AddDbContext`, Connection String, Retry-/Timeout-Policy oder Migration-Strategie.
- **DbContext-Dateilage:** Die Schicht ist korrekt, ein eigener Ordner `Persistence` sowie separate `IEntityTypeConfiguration<T>` würden Konfiguration und Migrationen klarer machen. Dies ist P2, kein Blocker.

## 10. DevOps- und Betriebsfähigkeit

| Bereich | Status | Befund |
|---|---|---|
| Docker/Compose | **Fehlt** | Kein Dockerfile, keine Compose-Datei, keine PostgreSQL-/TimescaleDB-Servicebeschreibung oder Healthchecks. |
| Konfiguration | **Nicht ausreichend** | Keine `appsettings*.json`, Options-Klassen oder Environment-Variable-Bindung; lokale absolute Pfade und DB-Zugang im Code. |
| Logging | **Nicht ausreichend** | Ausschließlich `Console.WriteLine`; kein `ILogger`, keine strukturierten Import-/Correlation-/Job-IDs. |
| Fehlerbehandlung | **Nicht ausreichend** | Reader-/Workbook-Ausnahmen laufen ungeordnet hoch; CSV-Fehler werden teils still verworfen; keine standardisierten Result-/Issue-Rückgaben. |
| Tests | **Fehlt** | Keine Unit-, Integration-, Architektur-, Migrations- oder End-to-End-Testprojekte. |
| CI/CD | **Fehlt** | Keine Pipeline, keine Solution, kein SDK-Pin, kein zentraler Package-Pin, keine automatischen Qualitäts-/Security-Prüfungen. |
| Worker-Betrieb | **Nicht produktionsfähig** | Kein Generic Host, DI, Graceful Shutdown, Healthcheck, Retry, Idempotenz oder konfigurierbarer Input; Programm ist Testcode. |
| Repository-Hygiene | **Unzureichend** | Keine `.gitignore` sichtbar; `bin/` und `obj/` sind lokal vorhanden. Binäre Ein-/Ausgabedaten liegen im Repository. |

Testbarkeit ist auf Klassenebene teilweise vorbereitet, da Reader/Writer/Lookup-Interfaces vorhanden sind. Der statische Validator, statische ID-Generatoren, Service-Locator in der Factory und fehlende Application-Orchestrierung erschweren isolierte Tests. Ein erster CI-Schritt kann ohne Architekturänderung Build, Unit Tests, Architekturtests und Formatprüfung ausführen.

## 11. Erkannte Abweichungen

### Kritisch für Baseline v1.0

1. Worker umgeht Application und schreibt trotz `Abort` weiter.
2. Benutzerentscheidung und getrennte Modelle für Issue, Duplicate Candidate und Resolution fehlen.
3. Data Products und deren exklusive Konsumschnittstelle fehlen vollständig im Code.
4. Produktiver, provenance-sicherer Import mit Raw-/Curated-Gate fehlt.
5. Konfiguration enthält hardcodierte Pfade und Datenbankzugang; kein sicherer Composition Root.

### Wesentlich, aber nach dem unmittelbaren Gate lösbar

1. TimescaleDB-Hypertable ist nicht provisioniert/nachgewiesen.
2. ImportJob/DataSource wurden aus dem persistierten Modell entfernt; `SourceImportJobId` bleibt ohne FK.
3. CSV-Parser kann fehlerhafte Daten still verwerfen oder in Nullwerte umdeuten.
4. Keine automatisierten Tests, Containerumgebung oder CI.
5. Migration zur eindeutigen `MeterNumber` ist für vorhandene Daten nicht robust.

### Dokumentationsabweichungen

- README nennt nur drei Backend-Projekte und bezeichnet Worker/ETL als offen, obwohl `Enset.Worker.Import` existiert.
- Mehrere Dokumente sprechen von implementierter TimescaleDB-Persistenz; implementiert ist aktuell EF/Npgsql-Kompatibilität, nicht die Hypertable-Erzeugung.
- `docs/Decisions/adr-001-architecture.md` ist leer.
- Dokumentierte Dateipfade für CSV Reader/Factory stimmen nicht mehr mit der Repository-Struktur überein.

## 12. Notwendige kleine Anpassungen

Diese Änderungen bilden den minimalen Architekturabgleich; sie sind bewusst kein großer Refactor:

1. In Application einen vollständigen `IImportUseCase`/`ImportCoordinator` mit Ergebnis- und Zustandsmodell definieren; `IImportService` aus Infrastructure dorthin verlagern/ersetzen.
2. Worker in einen dünnen Composition Root umwandeln: Konfiguration und DI aufbauen, genau den Application Use Case aufrufen, keinerlei konkrete Reader-/Writer-Prozesslogik enthalten.
3. Beim Status `Abort` oder `AwaitingResolution` jeden Excel-/DB-Write technisch sperren. Testschreibcode aus dem produktiven Einstiegspunkt entfernen oder in ein separates Test-/Sample-Projekt verschieben.
4. Minimale getrennte Records/DTOs für `ImportIssue`, `DuplicateCandidate` und `ResolutionAction` plus `ResolutionStatus` einführen. Writer benötigt einen validierten, freigegebenen Import-/Resolution-Plan.
5. Einen Application-Port für unveränderliche Raw-Ablage und einen separaten Curated Writer definieren. Originaldatei nie überschreiben.
6. Einen minimalen versionierten `DataProduct`-Vertrag und Query-/Publish-Port definieren. Künftige API/Business Modules dürfen nur diesen Query-Port verwenden.
7. Connection String und Dateipfade in Options/Environment Variables verschieben; Geheimnisse aus dem Repository entfernen.
8. Timescale-Entscheidung explizit treffen und migrationstechnisch umsetzen bzw. PostgreSQL-only korrekt benennen.

Diese Anpassungen greifen nicht in EMS oder Data Space ein und halten externe Systemlogik in Infrastructure-Adaptern.

## 13. Optionale Verbesserungen

- Domain-Enums konsequent namespaces zuordnen und Ordner-/Namespace-Namen angleichen.
- EF-Konfigurationen in `Persistence/Configurations` aufteilen.
- `CalculationResult` um Berechnungsmethode/-version, Input-Provenance, Qualitätsstatus und reproduzierbaren Run erweitern.
- `BenchmarkDataset` um Datenquelle, Dataset-Version, Gültigkeitszeitraum und Freigabestatus erweitern.
- Service-Locator in `MeterReadingReaderFactory` durch explizite Adapterregistrierung oder typisierte Factory ersetzen.
- Streaming/Batching für große Zeitreihenimporte, Transaktionen und idempotente Upsert-Strategie ergänzen.
- Raw-/Produkt-Binaries aus Git herausnehmen und mit Test-Fixtures bzw. externem Object Storage arbeiten.
- ADR 001 mit verbindlichen Schicht-, Zonen- und Data-Product-Grenzen füllen.
- Observability um Metriken, Tracing und Import-Job-Dashboard ergänzen.

## 14. Priorisierte To-do-Liste

### P0 – muss vor fachlicher Weiterarbeit bzw. Nutzung des Importpfads behoben werden

- [ ] Worker-Write nach `ImportDecisionType.Abort` entfernen und Write-Gate automatisiert testen.
- [ ] Worker ausschließlich über einen Application Import Use Case führen; direkte konkrete Reader-/Writer-Nutzung aus dem Ablauf entfernen.
- [ ] `ImportIssue`, `DuplicateCandidate`, `ResolutionAction` und einen persistier-/auditierbaren Benutzerentscheid modellieren.
- [ ] Excel-/DB-Aktualisierung nur nach explizit bestätigter Resolution zulassen; automatische Dubletten-Merges ausdrücklich verbieten.
- [ ] Minimalen Data-Product-Vertrag und Application Query-Port als einzige künftige Business-Module-Schnittstelle festlegen.
- [ ] Connection String, Credentials und Dateipfade externalisieren; hart codierte Werte entfernen.
- [ ] CSV-Parser so ändern, dass ungültige Zeilen/Werte als Issues zurückgegeben und nie still als `0` oder Skip kuratiert werden.

### P1 – sollte im nächsten Schritt behoben werden

- [ ] Raw Store, Curated Writer und Provenance inklusive persistentem ImportJob/DataSource-Konzept implementieren.
- [ ] TimescaleDB-Hypertable-Migration plus Extension-/Deployment-Prüfung erstellen oder PostgreSQL-only als bewusste MVP-Entscheidung dokumentieren.
- [ ] Bestandsrobustes Backfill vor Unique Index auf `MeterNumber` definieren.
- [ ] Unit Tests für Validator/Decision/Resolution, Architekturtests für Projektgrenzen sowie DB-/Migrationsintegrationstests ergänzen.
- [ ] Worker mit Generic Host, DI, Options, `ILogger`, Cancellation und eindeutigen Exit Codes betreiben.
- [ ] Dockerfile und Compose für Worker plus PostgreSQL/TimescaleDB mit Healthcheck und persistentem Volume ergänzen.
- [ ] CI für Restore, Build, Test, Architekturtests und Migrationsprüfung einrichten.
- [ ] Data-Product-Erzeugung als explizite Stufe nach Validierung, Berechnung/Benchmark und Quality Gate implementieren.

### P2 – später verbessern

- [ ] Namespaces, leere Platzhalter und Ordnerbenennung bereinigen.
- [ ] EF-Konfigurationen modularisieren und Constraints/Precision/UTC-Regeln vervollständigen.
- [ ] Solution/SDK-/Package-Versionen zentral verwalten und EF-Paketstände angleichen.
- [ ] Analyse- und Benchmark-Provenance/Versionierung erweitern.
- [ ] ADR, README und Detaildokumentation mit dem realen Implementierungsstand synchronisieren.
- [ ] Observability, Performance-/Lasttests, Retention, Backup/Restore und Disaster-Recovery testen.

## 15. Abschlussentscheidung

**Code benötigt Anpassungen vor Version 1.0.**

Mit der bestehenden Grundstruktur kann weitergearbeitet werden; ein Neuaufbau ist nicht erforderlich. Vor der nächsten fachlichen Erweiterung sollte jedoch zuerst der P0-Architekturabgleich umgesetzt werden. Insbesondere darf `Enset.Worker.Import/Program.cs` in seinem aktuellen Zustand nicht als produktiver Import verwendet werden. Sobald der Application-gesteuerte, benutzerfreigegebene Importpfad und der Data-Product-Port stehen, ist die Architektur tragfähig für die weitere MVP-Entwicklung.

## 16. Änderungen im Rahmen dieses Reviews

| Datei | Grund | Architekturprinzip | Risiko | Auswirkung |
|---|---|---|---|---|
| `docs/architecture/ARCHITECTURE_REVIEW_V1_0.md` | Vollständige Prüfung und dokumentierte Entscheidung | alle sechs Baseline-Prinzipien | sehr gering; nur Dokumentation | schafft verbindlichen Befund und priorisierte Umsetzungsliste |

Es wurden keine Produktions-, Domain-, Persistenz- oder Migrationsdateien verändert und keine Refactorings durchgeführt.
