# DATA_LINEAGE_ANALYSIS_V1_0

Status: Working Draft  
Version: 1.0  
Stand der Codeanalyse: 30.07.2026  
Ziel: Analyse der tatsächlichen Datenverarbeitung vom Import bis zu Internal
Data Products und Benutzeroberfläche.

## 1. Zielsetzung und Abgrenzung

Dieses Dokument beschreibt den implementierten MVP, nicht nur das
architektonische Zielbild. Analysiert wurden die tatsächlich aufgerufenen
Reader, Importmodelle, Validatoren, Resolution- und Commitpfade,
EF-Persistenz, Curation und Gold Profiles, Internal Data Products, REST-API
und React-Oberfläche.

Bewertung:

- ✅ konsistent: Quelle, Speicherung und Anzeige sind fachlich nachvollziehbar.
- ⚠ eingeschränkt oder mehrdeutig: Daten sind vorhanden, werden aber
  unterschiedlich abgeleitet, nur teilweise übertragen oder irreführend
  bezeichnet.
- ❌ inkonsistent: Information geht verloren, wird fachlich falsch belegt oder
  der implementierte Pfad widerspricht dem dokumentierten Zielbild.
- ⏳ nicht implementiert: Das Modell kann das Thema teilweise abbilden, der
  Import-/Produktpfad existiert jedoch nicht.

Es wurden keine Laufzeitdaten einer konkreten Datenbank bewertet. Aussagen
beziehen sich auf das Verhalten des Codes.

## 2. Tatsächlicher Datenfluss

```text
CRM Excel ──> ExcelWorkbookReader ───────────────┐
Lastprofil CSV ──> CsvMeterReadingReader ────────┼─> ImportWorkbook
LEB CSV ──> LebWorkbookReader/LebWorkbookMapper ─┘
       -> Import-DTOs
       -> ExcelImportValidator oder LebImportValidator
       -> DuplicationCheckService / ImportIssue
       -> ApplyResolutionService
       -> ImportWriteGate
       -> DatabaseImportWriter
       -> relationale Tabellen + rohe ImportedMeterReadings
       -> CuratedFieldValues/CurationTasks
       -> BuildingGoldProfile/MeteringPointGoldProfile
       -> optionaler GoldProfileVersion-Snapshot
       -> Internal Data Products
       -> REST
       -> Dashboard bzw. Kunden-/Objekt-/Zählpunktseiten
```

Wichtige Präzisierung: Der letzte Teil ist im Code **keine durchgehende
Pipeline `Gold-Snapshot -> Internal Data Product`**. Internal Data Products
fragen überwiegend die relationalen Tabellen und `CuratedFieldValues` direkt
ab. Von `GoldProfileVersions` übernehmen sie hauptsächlich Metadaten wie
Version, Release-Status und Hash. Der Inhalt von `SnapshotJson` wird nicht als
fachliche Quelle der Summary Products gelesen.

## 3. Importpfade

### 3.1 CRM Excel

`ExcelImportAnalysisService` erlaubt `.xlsx` und `.xlsm` und erstellt einen
`ExcelImportReader` mit `ExcelWorkbookReader`.

| Worksheet | Pflicht/Spalten | Import-Zwischenmodell |
|---|---|---|
| `Customers` | `ExternalCustomerId`, `CompanyName`; weitere Kontakt- und Adressfelder optional | `CustomerExcelRow` |
| `Buildings` | `ExternalBuildingId`, `ExternalCustomerId`; Name, Adresse, Typ optional | `BuildingExcelRow` |
| `Meters` | gemeinsam mit `MeterReadings` vorhanden oder beide nicht vorhanden; `MeterNumber` Pflicht | `MeterExcelRow` |
| `MeterReadings` | `MeterNumber`, `Timestamp`, `Value` | `MeterReadingExcelRow` |

Legacy-Aliase wie `InternalCustomerId`, `InternalBuildingId` und
`OrganizationName` werden akzeptiert. `CustomerImportMapper` überführt die
Zeilen in die vier Import-DTO-Listen des `ImportWorkbook`.

### 3.2 Lastprofil CSV

`CsvMeterReadingReader` erkennt Semikolon, Komma oder Tabulator sowie
Synonyme für Zeitpunkt, Wert, Qualität, Zählernummer und Einheit. Die
Dateiendung ist trotzdem ausschließlich `.csv`.

Die Originalzeile bleibt zunächst in `CsvMeterReadingMapping.RawRows`
erhalten. `CsvMeterReadingMappingService` erzeugt
`MeterReadingExcelRow`; danach erzeugt `MeterReadingExcelRowMapper`
`MeterReadingImportDto`.

Erhalten bleiben:

- Rohwerte für Zählernummer, Zeitpunkt, Wert und Qualität;
- physische Zeilennummer;
- erkannte beziehungsweise ausgewählte Spalten;
- Parsingfehler;
- Herkunft eines Felds (`FileColumn`, `Generated`, `Missing`).

Verloren beziehungsweise reduziert werden:

- beliebige Zusatzspalten gelangen nicht in `MeterReading` und nur über das
  Mapping im flüchtigen Report weiter;
- `Unit` wird nicht am kuratierten `MeterReading` gespeichert, sondern vom
  zugeordneten `Meter` erwartet;
- ein fehlender Zeitstempel kann im Resolutionpfad generiert werden, ist aber
  anschließend im Rohdatensatz als `TimestampRaw = null` erkennbar.

### 3.3 Landesenergiebuchhaltung CSV

`LebWorkbookReader` verarbeitet nur `.csv`, erkennt wiederholte Header,
Semikolon/Komma/Tabulator, UTF-8 beziehungsweise Windows-1252 und bewahrt
physische Quellspalten in `LebSourceColumn`.

`LebWorkbookMapper` transformiert LEB-Zeilen wie folgt:

| LEB-Quelle | Transformation | Importziel |
|---|---|---|
| `GemID` | `LEB:GEM:{GemID}` | `CustomerImportDto.ExternalCustomerId` |
| Gemeindename | erster nicht leerer Wert, sonst `GemID` | `CompanyName` |
| `GebID` | `LEB:GEM:{GemID}:GEB:{GebID}` | `ExternalBuildingId` |
| Gebäudename | unverändert | `BuildingName` |
| `ZId` | `LEB:GEM:{GemID}:GEB:{GebID}:Z:{ZId}` | `MeterNumber` |
| ausgewähltes Medium | `Electricity` oder `Heat` als `ProfileName` | später `Meter.Name` und `Meter.Medium` |
| `Jan` bis `Dez` | je vorhandenem Wert ein Zeitstempel am ersten Monatstag, 00:00 UTC | `MeterReadingImportDto` |
| `AnnualTotal` | validiert, aber nicht in `ImportWorkbook` übernommen | verloren |
| Baujahr, Fläche | zunächst `BuildingExcelRow`, aber nicht im `BuildingImportDto` | vor Persistenz verloren |

Damit wird die originale `ZId` nicht als sichtbare Zählpunktnummer gespeichert.
Stattdessen entsteht eine zusammengesetzte technische Kennung. Das ist
deterministisch und kollisionsarm, aber fachlich keine Original-
Zählpunktnummer.

## 4. Validation, Resolution, WriteGate und ImportReport

### 4.1 Validation

`ExcelImportValidator` validiert CRM- und Lastprofil-Importmodelle.
`LebImportValidator` ergänzt LEB-Pflichtfelder, Zahlenformate und
Quellspaltenprobleme. `DuplicationCheckService` ergänzt Dubletten- und
Referenzprobleme. Der Report wird durch `RecalculateCommitReadiness` auf
`AwaitingResolution` oder `ReadyToCommit` gesetzt.

### 4.2 Resolution

`ApplyResolutionService` verändert den im `ImportReport` enthaltenen
Import-Payload und/oder Issues. Einzel- und Gruppenentscheidungen werden mit
Quelle, Benutzer und Zeitpunkt protokolliert. Die Resolution arbeitet vor dem
WriteGate und kann deshalb den später geschriebenen DTO-Wert beeinflussen.

### 4.3 WriteGate und Writer

`ImportCommitService` lädt den Report, prüft Status und offene blockierende
Issues und übergibt einen `ImportWriteContext` an `ImportWriteGate`.
`DatabaseImportWriter` schreibt anschließend:

- `Customer`;
- `Building`;
- `CustomerBuildingAssignment`;
- `Meter`;
- jede Messwertzeile als `ImportedMeterReading`;
- nur gültige, zugeordnete Zeilen zusätzlich als `MeterReading`.

Für `(MeterId, Timestamp)` existierende kuratierte Messwerte werden nicht
aktualisiert; weitere Importe derselben Kombination werden übersprungen.

### 4.4 Persistenzlücke des ImportReport

`ImportReportEntity` persistiert nur einen Teil des fachlichen Reports:

- Import-ID, Benutzer-/Kunden-ID, Status;
- Quelldateimetadaten;
- Customer- und Building-Count;
- Issues und Audit Trail.

Nicht in `ImportReportEntity` gespeichert werden unter anderem:

- `SourceType`;
- Default-/zugeordnete Zähler-ID;
- CSV-Mapping;
- Customer-, Building-, Meter- und MeterReading-Payload;
- LEB-Quellspalten;
- Meter- und MeterReading-Count;
- Resolution Rules und Decision.

`ImportReportPersistenceMapper.ToModel` kann diese Informationen nach einem
Reload folglich nicht rekonstruieren. `ImportQualityProduct.ImportType`
verwendet sogar `SourceFileContentType` statt `SourceType`. Das ist eine
fachliche Lineage-Lücke.

## 5. Customer

| Feld | Importquelle/Transformation | Datenbank | Gold / Product / REST | UI | Bewertung |
|---|---|---|---|---|---|
| GUID | nicht importiert; `BaseEntity.Id` wird erzeugt | `Customer.Id` | `CustomerSummaryProduct.CustomerId` | Routing/Details | ✅ interne Identität |
| Kundennummer | CRM `ExternalCustomerId`; LEB generiert aus `GemID` | `Customer.CustomerNumber` | `CustomerSummaryProduct.CustomerNumber`; CRUD DTO | Liste und Detail | ✅ technisch stabil; ⚠ LEB-Wert ist technische Gemeindeidentität |
| Kundenname | CRM `OrganizationName/CompanyName`, sonst Vor-/Nachname; LEB Gemeindename | `Customer.Name`, zusätzlich `LegalName = CompanyName` | `OrganizationName` im Product ist tatsächlich `Customer.Name` | Kundenliste/-detail; Building/Meter-Zuordnung | ✅ für CRM; ⚠ LEB verwendet Kunde als Gemeinde |
| Kontaktperson | CRM-Spalte vorhanden | `Customer.ContactPerson` existiert, wird vom Importwriter jedoch **nicht gesetzt** | Product liest `Customer.ContactPerson` | Kundendetail | ❌ Information geht beim Import verloren |
| Straße/Hausnummer | CRM-Spalten; LEB leer | `Customer.Street/HouseNumber` | nicht im CustomerSummaryProduct, aber CRUD Detail | Kundendetail | ✅ CRM-Persistenz |
| PLZ/Ort | CRM `PostalCode/City`; LEB leer | `Customer.PostalCode/City` | CustomerSummaryProduct und REST | Liste/Detail zeigen Ort | ✅ CRM; für LEB fachlich nicht vorhanden |
| Land | CRM; LEB `"AT"` | `CountryCode`; Normalisierung, unbekannte Werte fallen auf `AT` zurück | nur CRUD Detail | Detail | ⚠ unbekannte Länder werden still zu AT |
| E-Mail/Telefon | CRM-Spalten | `Customer.Email/Phone` | CustomerSummaryProduct | Liste/Detail | ✅ |
| Gemeinde | LEB `GemID`/Gemeindename wird als Customer modelliert | keine explizite Municipality-Beziehung am Customer | LEB-Export ermittelt Gemeinde stattdessen über BuildingVersion.Address | UI zeigt Gemeinde als normalen Kunden | ❌ Import- und Exportmodell verwenden unterschiedliche Gemeindepfade |

Die Kundendarstellung ist nicht quelltypspezifisch. Nicht-LEB und LEB werden
über dieselben `Customer`-Felder angezeigt; eine explizite Trennung zwischen
Firma und Gemeinde existiert nicht.

## 6. Building

| Feld | Importquelle/Transformation | Datenbank | Gold / Product / REST | UI | Bewertung |
|---|---|---|---|---|---|
| GUID | vom Writer erzeugt | `Building.Id` | alle Produkte/DTOs | Routing | ✅ |
| Gebäudenummer | CRM `ExternalBuildingId`; LEB zusammengesetzte ID | `Building.BuildingNumber`, zusätzlich `ExternalIdentifier` identisch | `BuildingSummaryProduct.BuildingNumber` | Liste/Detail | ✅ stabil; ⚠ LEB nicht originale `GebID` |
| Objektname | CRM `BuildingName/ProjectName`; LEB Gebäudename, Fallback Gebäudenummer | `Building.Name` | Product/REST direkt | Liste/Detail | ✅ |
| Kunde | Importreferenz; Writer erzeugt `CustomerBuildingAssignment` | Relation mit Rolle `Unknown`, `IsPrimary=false` | jeweils erste aktive Zuordnung; CRUD sortiert teils nach `IsPrimary` | Nummer und Name | ⚠ Auswahl bei mehreren nicht überall gleich; keine quelltypspezifische Darstellung |
| Gebäudetyp | CRM `BuildingType`; LEB Quelltyp vorhanden | Writer erzeugt/aktualisiert **keine BuildingVersion** | Product liest `BuildingVersion.BuildingCategory` | Liste | ❌ Importwert geht vor DB verloren |
| Nutzungstyp | diverse Importmodelle besitzen Werte, `BuildingImportDto` jedoch nicht | `BuildingVersion.PrimaryUseType` nur CRUD/Curation | Product liest BuildingVersion | Liste/Gold | ❌ nicht aus Import persistiert |
| Gebäudezustand | Import und CRUD | `BuildingState` als kuratiertes Fachfeld | Summary und CRUD lesen dasselbe Curation-Feld | Liste | ✅ konsistent |
| Adresse | CRM in `BuildingImportDto`; LEB teilweise in Quellzeile | Writer ignoriert alle Building-Adressfelder | BuildingVersion/Address nur CRUD | Product und UI | ❌ Importadresse geht verloren |
| Baujahr | LEB/ältere Excelmodelle vorhanden | Writer ignoriert es; BuildingImportDto besitzt kein Baujahr | BuildingVersion/Gold/Export | Detail | ❌ vor Persistenz verloren |
| Fläche | LEB `m2` in `BuildingExcelRow` | nicht im BuildingImportDto, keine BuildingVersion | Gold/LEB-Export erwarten Flächen | Detail/Readiness | ❌ vor Persistenz verloren |

## 7. Meter / Zählpunkt

| Feld | Importquelle/Transformation | Datenbank | Gold / Product / REST | UI | Bewertung |
|---|---|---|---|---|---|
| GUID | Writer erzeugt | `Meter.Id` | `MeteringPointId` | Routing | ✅ |
| Zählpunktnummer | CRM `MeterNumber`; Lastprofil Spalte/Default; LEB **generiert** aus Gemeinde, Gebäude und `ZId` | `Meter.MeterNumber` | Product `MeteringPointNumber`; CRUD DTO | „Zählpunktnummer“ | ❌ für LEB keine originale Zählpunktnummer |
| Interne ID | kein separates fachliches Importfeld | `Meter.Name` wird aus `ProfileName` oder MeterNumber gesetzt | Product nennt es `InternalName` | UI zeigt `item.name` als „Interne ID“ | ❌ Name ist keine GUID; bei LEB häufig `Electricity`/`Heat` |
| Externe Referenz | nicht durch Importwriter gesetzt | `Meter.ExternalIdentifier` | LEB-Export verwendet es als `GridMeteringPointNumber` | Detail | ❌ importierte Zählpunktreferenz wird nicht dorthin übernommen |
| Objekt | CRM/LEB `ExternalBuildingId`, Lastprofil optional Ziel-GUID | `Meter.BuildingId` | Product/REST | Nummer und Name | ✅ bei eindeutiger Referenz |
| Kunde | indirekt über aktive Building-Zuordnung | keine direkte Meter-Customer-Relation | unterschiedliche First/Primary-Abfragen | Nummer und Name | ⚠ abgeleitet und bei Mehrfachzuordnung nicht überall deterministisch |
| Energieträger | LEB Medium wird in `ProfileName` transportiert | Writer mappt nur `Electricity`/`Heat`; sonst bleibt `Unknown` | Product/REST | lokalisiertes Label | ⚠ LEB korrekt für zwei Medien; CRM-Profilname wird zweckentfremdet |
| Richtung | kein Importmapping | initial `Unknown` | Product/REST | UI zeigt `Unknown` | ❌ Importpfad setzt Richtung nicht |
| Einheit | CRM/CSV/LEB | `Meter.Unit`, unbekannte Werte werden `Unknown` | Product/REST; Messwerte erben Meter-Einheit | UI | ✅ mit begrenztem Mapping |
| Quantity/Typ | Import-DTO besitzt die fachlichen Felder nicht | Writer setzt beide initial `Unknown` | Gold/Readiness und Jahreswertlogik verwenden Quantity/Type | Detail | ❌ |
| Jahreswert | Import setzt `Meter.AnnualValue` nicht; LEB `AnnualTotal` geht verloren | nullable `AnnualValue`, `AnnualValueOrigin` | zwei unterschiedliche Ableitungen, siehe Abschnitt 11 | Liste/Detail/Dashboard | ❌ |

## 8. MeterReading

| Feld | Quelle/Transformation | Raw-Persistenz | kuratierte DB | Product/REST/UI | Bewertung |
|---|---|---|---|---|---|
| Zählerbezug | Nummer oder Ziel-GUID | `MeterId`, `MeterNumberRaw` | `MeterReading.MeterId` | implizit über Route | ✅ Rohbezug bleibt erhalten |
| Zeitpunkt | CSV/Excel oder LEB erster Monatstag; mehrere Kulturen | `TimestampRaw` und geparster `Timestamp` | UTC-normalisiert | Min/Max, Liste, Gold | ✅; ⚠ LEB repräsentiert Monatswert nur durch Monatsanfang |
| Wert | CSV/Excel/LEB; deutsches/invariantes Dezimalformat | `ValueRaw` und decimal | `MeterReading.Value` | Detail und Export | ✅ |
| Einheit | Quelldatei | nicht in `ImportedMeterReading` | nicht in `MeterReading` | wird vom Meter gelesen | ⚠ zeilenbezogene Abweichungen gehen verloren |
| QualityFlag | Ganzzahl aus Import | roh und geparst | Mapping auf `DataQuality`, unbekannt -> `Unknown` | Detail, Gold-Zähler, Export | ✅ mit Informationsreduktion |
| ReadingType | nicht aus Import übernommen | nicht vorhanden | immer `Unknown` | Jahreswertfilter/LEB-Exportmapping | ❌ Intervall-/Zählerstandsemantik geht verloren |
| IntervalSeconds | nicht im Import-DTO | nicht vorhanden | Importwriter setzt es nicht | Gold/Product versuchen daraus Intervall und Coverage zu bestimmen | ❌ importierte Reihen haben meist kein bestimmbares Intervall |
| Herkunft | Import-ID, Datei, Raw-ID | Import-ID/SourceName | `SourceImportJobId`, `SourceRawReadingId`, `DataOrigin` | Detail zeigt DataOrigin | ✅ |
| Parsingfehler | Reader/Mapper | `ParsingError` | fehlerhafte Zeile wird nicht kuratiert | Import-Issues | ✅ Raw-Zone bleibt prüfbar |

### Zeitraum und Vollständigkeit

`MeterSummaryProduct` verwendet `Min(Timestamp)` und `Max(Timestamp)`.
`ExpectedValueCount`, fehlende Werte und Vollständigkeit werden nur berechnet,
wenn mindestens ein `IntervalSeconds` vorhanden ist. Es wird das erste
vorhandene Intervall verwendet; Gold Profile verwendet dagegen das häufigste
Intervall. Das kann unterschiedliche Ergebnisse erzeugen.

Das CRUD-ReadModel zeigt Anzahl sowie erstes/letztes Datum unabhängig von
Intervall und Coverage. Die Listenanzeige lautet aktuell
`Anzahl · Start – Ende`, nicht mit dem im Entwurf genannten senkrechten
Trennzeichen.

## 9. EnergySystem

Ein Importpfad existiert nicht. `DatabaseImportWriter` enthält dazu einen
TODO. EnergySystems werden ausschließlich über CRUD in `EnergySystem` und
`EnergySystemBuildingAssignment` gepflegt.

Internal Data Products:

- BuildingSummaryProduct enthält nur meterbasierte Energiewerte und zählt
  keine Anlagendetails.
- CustomerSummaryProduct zählt zugeordnete EnergySystems.
- PortfolioSummaryProduct enthält keine EnergySystem-Felder.

`NoeLebExportContractV1` liest dagegen Anlagen direkt aus relationalen
EnergySystem-Zuordnungen und mappt Typ, Leistung und Gültigkeit. Das
Anlagenbaujahr existiert nicht und wird im Export immer `null`.

Bewertung: ⏳ CRUD und Export vorhanden, aber kein Import-/Gold-/Summary-
Lineagepfad.

## 10. Gold Profiles

### 10.1 BuildingGoldProfile

`EfCurationService.GetBuildingProfileAsync` kombiniert:

- relationale Building-/BuildingVersion-/Address-Daten;
- aktive Customer-Zuordnung;
- aktuelle `CuratedFieldValues`;
- daraus berechnete FieldReadiness.

Curation-Werte überschreiben relationale Fallbacks. Ein Gold Profile enthält
unter anderem Nutzung, beheizte Fläche, PLZ, Verbrauch/Produktion, HWB,
Gebäudezustand und Klassifikationen.

### 10.2 MeteringPointGoldProfile

Das MeteringPoint-Profil kombiniert Meter-Stammdaten, Building/Customer,
CuratedFieldValues und aggregierte Messwertmetadaten. Erwartete Werte,
Coverage und Qualitätsanteile werden zur Laufzeit berechnet.

### 10.3 Versionierung und Verwendung

`GoldProfileVersionService.Create` serialisiert das aktuelle Curation-Profil
als `SnapshotJson`, bildet SHA-256 und versioniert es. Release ist nur bei
Gold-Readiness möglich.

Die Summary Products lesen den Snapshotinhalt jedoch nicht. Sie verwenden:

- `GoldProfileVersions` für Version/Status/Hash und Zählungen;
- `CuratedFieldValues` für Maturity und einzelne Felder;
- relationale Tabellen für fast alle fachlichen Werte.

Damit ist das dokumentierte Ziel
`Gold Profiles -> Internal Data Products` nur teilweise umgesetzt.

### 10.4 Gold-Maturity

`Maturity` zählt alle aktuellen CuratedFieldValues je MaturityLevel und
berechnet `Gold / (Bronze + Silver + Gold)`. Die CRUD-Listen verwenden
abweichende Regeln:

- Building: genau fünf ausgewählte Gold-Felder, je 20 Prozent;
- Meter: Anzahl sämtlicher Gold-Felder multipliziert mit `100 / 9`;
- Product: Anteil Gold an allen vorhandenen Curation-Feldern.

Dieselbe Bezeichnung „Gold-Reife“ kann deshalb unterschiedliche Prozentwerte
zeigen.

## 11. Jahreswert – tatsächliches Verhalten

Es bestehen zwei voneinander unabhängige Pfade.

### 11.1 CRUD-Listen und -Details

`EfEntityReadService.GetMetersAsync/GetMeterAsync` verwendet:

1. `Meter.AnnualValue`, falls vorhanden;
2. sonst eine Summe von Messwerten mit `ReadingType.IntervalValue`, wenn
   `Meter.Quantity == Energy` und `Max(Timestamp)-Min(Timestamp) >= 364 Tage`;
3. andernfalls `null`.

Es gibt keine Prüfung auf zwölf vorhandene Monate, erwartete Intervalle,
fehlende Werte oder Coverage. Ein Zeitraum von mindestens 364 Tagen genügt.
Es erfolgt keine Hochrechnung; vorhandene IntervalValues werden einfach
aufsummiert.

| Datenlage | Ergebnis im CRUD-ReadModel |
|---|---|
| 3 Monate | kein berechneter Jahreswert |
| 8 Monate | kein berechneter Jahreswert |
| 11 Monate | normalerweise kein Wert; bei ungewöhnlich weit auseinanderliegenden Zeitpunkten kann die 364-Tage-Regel trotzdem greifen |
| 12 Monate | nur bei Zeitspanne >= 364 Tage, Quantity `Energy` und ReadingType `IntervalValue`; dann einfache Summe |

Für aktuell importierte Messwerte sind `Quantity` und `ReadingType` meist
`Unknown`. Daher greift die Berechnung typischerweise auch bei zwölf Monaten
nicht.

### 11.2 Internal Data Products und Dashboard

`EfInternalDataProductService` verwendet ausschließlich
`Meter.AnnualValue`. Messwerte werden dort nicht zu einem Jahreswert
aggregiert. `AnnualValueOrigin` wird übernommen; `ReferencePeriod` bleibt bei
EnergySummaryItems und BuildingSummaryProduct `null`.

Das Dashboard summiert diese vorhandenen Werte gruppiert nach Medium,
Richtung und Einheit. Bei gemischten Einheiten werden Gesamtverbrauch und
Gesamterzeugung unterdrückt, die gruppierten Werte bleiben sichtbar.

Folgen:

- Ein im CRUD-ReadModel aus Messwerten berechneter Jahreswert kann in der
  Zählpunktliste sichtbar sein, aber im Internal Data Product und Dashboard
  fehlen.
- Es gibt keine Anzeige „Unvollständiges Jahr“.
- Coverage wird für den Jahreswert nicht berücksichtigt.
- Es findet keine Hochrechnung statt.
- LEB `AnnualTotal` wird weder als AnnualValue noch als Kontrollsumme genutzt.

Bewertung: ❌ P0.

## 12. Internal Data Products

### 12.1 BuildingSummaryProduct

Quelle sind `Building`, aktuelle offene `BuildingVersion`, erste aktive
Customer-Zuordnung, Meter.AnnualValue, CuratedFieldValues, CurationTasks,
GoldProfileVersions und dynamische Readiness.

Auffälligkeiten:

- Energiewerte stammen nicht aus dem Building-Gold-Snapshot.
- `ReferencePeriod` ist immer `null`.
- `ValueOrigin` ist pauschal `ExistingAnnualValue`, sobald irgendein
  Energiewert existiert.
- Customer-Auswahl ist nicht nach `IsPrimary` sortiert.

### 12.2 MeterSummaryProduct

Quelle sind `Meter`, Building/Customer, `MeterReadings`,
CuratedFieldValues/CurationTasks, Gold-Metadaten und Readiness.

Auffälligkeiten:

- `MeteringPointNumber = Meter.MeterNumber`;
- `InternalName = Meter.Name`, keine interne GUID;
- AnnualValue ausschließlich aus dem gespeicherten Meterfeld;
- Intervallwahl unterscheidet sich vom MeteringPointGoldProfile;
- „MeasuredValueCount“ wird als Restmenge berechnet und zählt dadurch auch
  `Unknown` als gemessen, solange es nicht in einer anderen Kategorie liegt.

### 12.3 CustomerSummaryProduct

Quelle sind Customer, aktive Building-Zuordnungen und deren Meter/Anlagen.
Energiewerte werden aus Meter.AnnualValue aggregiert. Der
`DataProductReadinessSummary` wird für alle Produkttypen nur mit
`NotAvailable` befüllt und nicht tatsächlich ausgewertet.

### 12.4 PortfolioSummaryProduct

Das Dashboard-Produkt aggregiert relationale Bestände, Meter.AnnualValue,
Curation, Gold-Metadaten und Importtabellen.

Auffälligkeiten:

- Building-/Energy-Benchmark-Readiness wird aus der Anzahl freigegebener
  Profile und einem hart codierten Wert `92` erzeugt;
- „HighPriorityCurationTaskCount“ wird konstant `0` geliefert;
- „MetersWithIncompleteProfiles“ zählt nur Meter mit Intervall und mindestens
  einem invaliden Wert, nicht alle Profile mit Coverage < 100 Prozent;
- Readiness ist keine Auswertung jedes Gold-Snapshotinhalts.

### 12.5 ImportQualityProduct

Quelle sind persistierte ImportReports, ImportIssues, CurationTasks und
GoldProfileVersions.

Auffälligkeiten:

- `ImportType` enthält den MIME-Type der Quelldatei, nicht `SourceType`;
- Ziel-Zählpunkt-ID/-nummer werden immer `null`;
- `CommittedAt` sucht Audit-Aktion `"Commit"`, während der Commit-Service
  `"CommitStarted"`, `"CommitCompleted"` und `"CommitFailed"` schreibt;
  `CommittedAt` bleibt deshalb im aktuellen Code `null`;
- fehlende Report-Payloads verhindern eine vollständige Lineage nach Reload.

## 13. REST und Benutzeroberfläche

Die Endpunkte unter `/api/v1/internal-data-products` geben die Application-
Records direkt als JSON zurück; es existieren dafür keine getrennten REST-
DTOs oder Mapper.

| Oberfläche | REST-Quelle | Tatsächliche Darstellung |
|---|---|---|
| Management Dashboard | PortfolioSummaryProduct + ImportQualityProduct | Portfolio-KPIs, vorhandene Jahreswerte, Datenqualität, Readiness, Importstatus |
| Kundenliste/-detail | CRUD ReadDtos; Detail zusätzlich CustomerSummaryProduct | Nummer, Name, Ort, Kontakte, Zählungen, aggregierte Gold-Reife |
| Objektliste/-detail | CRUD ReadDtos; Detail zusätzlich BuildingSummaryProduct | Nummer, Name, Typ, Nutzung, Kunde, Meter, Gebäudezustand, Reife |
| Zählpunktliste/-detail | CRUD ReadDtos; Detail zusätzlich MeterSummaryProduct | Nummer, als „Interne ID“ bezeichneter Name, Objekt/Kunde, Medium, Richtung, Jahreswert, Zeitraum, Messwerte, Gold/Readiness |
| Messwerttabelle | MeterReading-CRUD-REST | Zeitpunkt, Wert mit Meter-Einheit, Qualität, DataOrigin |

Die Entity-Listen verwenden nicht die gleichnamigen Internal Data Products,
sondern separate CRUD-ReadModels. Dadurch entstehen doppelte Ableitungen für
Jahreswert, Gold-Reife, Kundenwahl, Zeitraum und Intervall.

Die Zählpunkt-Detailseite besitzt Abschnitte für Stammdaten, Messwerte,
Curation Readiness, Gold-Versionen, Data-Product-Readiness, Upload und Audit.
Eine fachlich gegliederte Detailnavigation mit eigenem Lastprofil- und
Importhistorienbereich existiert nicht.

## 14. NoeLebExportContractV1

Der Contract Builder ruft zwar `PortfolioSummaryProduct` auf, verwendet davon
aber nur `CalculatedAt` als Exportzeitpunkt. Alle fünf Tabellen werden direkt
aus relationalen Tabellen aufgebaut:

| Contract-Tabelle | Reale Quelle |
|---|---|
| Municipalities | aktuelle BuildingVersion -> Address -> Municipality/Region |
| Objects | Building, aktuelle BuildingVersion, Address, aktive Customer-Zuordnung |
| Meters | Meter und erster ReadingType |
| Readings | MeterReading im optionalen Zeitraum |
| EnergySystems | EnergySystemBuildingAssignment und EnergySystem |

GoldProfile-Snapshots und kuratierte Feldwerte werden nicht gelesen.
`NoeLebExportContractV1` ist deshalb aktuell kein Export der freigegebenen
Gold-Snapshots, obwohl `docs/leb-export.md` den Fluss
`Gold Profiles -> Internal Data Products -> Contract` darstellt.

Weitere Auffälligkeiten:

- Gemeinde basiert auf Building-Adresse, während LEB-Import Gemeinde als
  Customer modelliert und keine BuildingVersion-Adresse schreibt.
- `GridMeteringPointNumber` stammt aus `Meter.ExternalIdentifier`, das der
  Importwriter nicht befüllt.
- `ReadingType` ist nach Import meist `Unknown` und wird im Export `null`.
- EnergySystem-Baujahr ist immer `null`.
- Validierung blockiert fehlende Flächen/Nutzung/Gemeinde, die der aktuelle
  Importwriter gerade nicht persistiert.

## 15. Architekturabweichungen

| Dokumentierte Aussage | Tatsächlicher Code | Bewertung |
|---|---|---|
| `Gold Profiles -> Internal Data Products` | Products lesen Relationstabellen und CuratedFieldValues; SnapshotJson bleibt ungenutzt | ❌ |
| `Gold Profiles -> Internal Data Products -> NoeLebExportContractV1` | Contract liest fast vollständig direkte EF-Entities; Product liefert nur Timestamp | ❌ |
| Jahreswerte sind vorhandene Meter.AnnualValue | Für Internal Products korrekt; CRUD-ReadModels besitzen zusätzlich eigene 364-Tage-Summenlogik | ⚠ doppelte Ableitung |
| Dashboard lädt PortfolioSummary und ImportQuality | Management Dashboard tut dies tatsächlich | ✅ |
| Internal Data Products sind requestbasierte ReadModels | trifft zu; sie sind nicht persistiert | ✅ |
| LEB ist fachlich vollständige Importquelle | Identitäten und Monatswerte gelangen durch; Building-Fachdaten und AnnualTotal gehen verloren | ❌ |

## 16. Bekannte Inkonsistenzen und Priorität

| Priorität | Bereich | Problem |
|---|---|---|
| P0 | Meter | LEB speichert eine generierte Verbund-ID statt originaler Zählpunktnummer |
| P0 | Meter | UI bezeichnet `Meter.Name` (`Electricity`, `Heat` oder Profilname) als „Interne ID“ |
| P0 | Jahreswert | LEB AnnualTotal geht verloren; Import setzt Meter.AnnualValue nicht |
| P0 | Jahreswert | CRUD und Internal Products verwenden unterschiedliche Ableitungen |
| P0 | Gold/Product | Internal Products konsumieren keine freigegebenen Gold-Snapshotwerte |
| P0 | Building | Typ, Nutzung, Adresse, Baujahr und Fläche aus Import werden nicht in BuildingVersion persistiert |
| P0 | ImportReport | SourceType und Import-Payload gehen bei EF-Persistenz/Reload verloren |
| P1 | MeterReading | Quantity, ReadingType und IntervalSeconds bleiben nach Import Unknown/null |
| P1 | Coverage | keine Jahreswertprüfung auf zwölf vollständige Monate oder Coverage |
| P1 | Gold-Reife | Product, Building-Liste und Meter-Liste berechnen Prozentwerte unterschiedlich |
| P1 | Kunde | mehrere aktive Building-Zuordnungen werden je Abfrage unterschiedlich priorisiert |
| P1 | LEB Export | Gemeinde-/Objektfelder stammen aus einem Pfad, den der LEB-Import nicht befüllt |
| P1 | ImportQuality | ImportType ist MIME-Type; Zielzähler fehlt |
| P1 | Readiness | Portfolio-Benchmarkwerte enthalten hart codierte 92 Prozent |
| P1 | Messwerte | Internal Product und Gold Profile wählen das relevante Intervall unterschiedlich |
| P2 | Customer | ContactPerson wird trotz Importfeld nicht vom Writer gespeichert |
| P2 | Customer | unbekannter Country-Wert fällt still auf AT zurück |
| P2 | EnergySystem | kein Import- oder Gold-/Summary-Pfad |
| P2 | UI | Messwertzeitraum nutzt `·` und Datumsbereich statt der gewünschten kompakten `Anzahl | Zeitraum`-Semantik |

## 17. Vollständig oder weitgehend konsistente Bereiche

- CRM-Kundennummer und -name gelangen nachvollziehbar in Customer und UI.
- Customer-Adresse, E-Mail und Telefon werden für CRM persistiert und
  angezeigt.
- Building- und Customer-GUIDs sind saubere interne Identitäten.
- Rohmesswerte bewahren wichtige Ursprungswerte, Zeilennummer, Import und
  Parsingfehler.
- Gültige Messwerte besitzen eine nachvollziehbare Raw-to-Curated-Verknüpfung.
- Messwert-Minimum/-Maximum und Anzahl werden direkt aus `MeterReadings`
  bestimmt.
- Dashboard-Portfoliozahlen für aktive und gesamte Customer/Building/Meter
  stammen aus klaren EF-Abfragen.
- Energiewerte werden in Internal Products nicht über inkompatible Einheiten
  hinweg zu einem irreführenden Gesamtwert addiert.
- LEB-CSV- und Excel-Export verwenden denselben Contract und dieselbe
  Validierung.

## 18. Empfohlene Zielstruktur

Ohne Vorgriff auf die Implementierung wird folgende Struktur empfohlen:

1. Pro fachlichem Feld eine kanonische Quelle definieren: Originalwert,
   kuratierter Wert und technische ID getrennt halten.
2. Originale LEB-Gemeinde-, Gebäude- und Zählerkennungen separat persistieren;
   zusammengesetzte Schlüssel nur als technische externe Identität verwenden.
3. Import-DTOs und Writer so ausrichten, dass BuildingVersion-Felder und
   fachliche Meterattribute nicht verloren gehen.
4. Eine einzige Jahreswertregel mit Referenzzeitraum, Coverage und Herkunft
   bereitstellen; CRUD und Products müssen dieselbe Projektion konsumieren.
5. Gold-Snapshot beziehungsweise eine eindeutig definierte aktuelle
   Gold-Projektion als fachliche Quelle der Internal Data Products verwenden.
6. NoeLebExportContractV1 aus derselben freigegebenen Projektion aufbauen oder
   die direkte relationale Quelle ausdrücklich als Vertragsregel festlegen.
7. Gold-Maturity und Readiness einmal zentral berechnen und unverändert an
   alle REST-/UI-Konsumenten liefern.
8. ImportReport so persistieren, dass SourceType, Zielzuordnung, Counts,
   Mapping und für Commit/Lineage erforderlicher Payload nach Reload
   rekonstruierbar bleiben.

## 19. Phase A – Umsetzungsstand

Stand: 30.07.2026

| Befund | Status | Umsetzung |
|---|---|---|
| BuildingVersion wird beim Import nicht befüllt | behoben | CRM- und LEB-Gebäudedaten werden in einer initialen beziehungsweise neuen historischen `BuildingVersion` gespeichert. Nicht gelieferte Werte werden aus der vorherigen aktiven Version übernommen und nicht durch null, 0 oder `Unknown` ersetzt. |
| LEB verwendet eine zusammengesetzte MeterNumber | behoben | `ZId` wird unverändert als `Meter.MeterNumber` verwendet; `Meter.Id` bleibt eine generierte GUID und der Zählername wird separat in `Meter.Name` gespeichert. |
| LEB AnnualTotal geht verloren | behoben | Der validierte Wert wird in `Meter.AnnualValue`, seine Herkunft in `AnnualValueOrigin` und das Bezugsjahr in `AnnualValueReferenceYear` gespeichert. Die Einheit bleibt in `Meter.Unit`. |
| ReadingType bleibt für bekannte Zeitreihen Unknown | behoben | Lastprofil- und monatliche LEB-Zeitreihen verwenden den bestehenden Typ `IntervalValue`; für fachlich nicht bestimmbare CRM-Quellen bleibt `Unknown` erhalten. |
| Quantity bleibt trotz eindeutiger Einheit Unknown | behoben | Eindeutige Einheiten werden auf `Energy`, `Power`, `Volume`, `Flow` und weitere bestehende Größen abgebildet. Unbekannte Einheiten bleiben `Unknown` und erzeugen bei der Importvalidierung eine Warnung. |
| IntervalSeconds fehlt | behoben | Ein Intervall wird nur bei einem expliziten Wert oder einem über die gesamte Zählerserie konstanten positiven Zeitabstand gespeichert. Gemischte Raster bleiben null und erzeugen eine Warnung. |
| Gebäudezustand | weiterhin offen | Das aktive Domainmodell besitzt kein fachlich geeignetes Feld in `BuildingVersion`. Im Rahmen von Phase A wurde hierfür keine parallele Struktur und keine Semantik erfunden. |

Für Phase A war eine minimale Migration erforderlich:

- `Meters.AnnualValueReferenceYear` speichert das Bezugsjahr des importierten
  LEB-Jahreswerts.
- `Addresses.City` erhält den importierten Ort unabhängig von der
  Postleitzahlbezeichnung.
- `PrimaryUseType`, `BuildingCategory` und `OwnershipType` in
  `BuildingVersions` sind nullable, damit fehlende Importwerte fachlich korrekt
  als null gespeichert werden können.

Die Änderung ist für bestehende Daten rückwärtskompatibel: Vorhandene Werte
bleiben unverändert; die neuen Spalten sind nullable. Die Rückmigration stellt
für die drei Enum-Spalten technisch leere Standardwerte her, weil PostgreSQL
beim Wechsel zurück auf `NOT NULL` einen Wert benötigt.

## 20. Phase B – Canonical Snapshot

Die fachliche Projektion für Customer, Building, Meter und EnergySystem ist in
`ICanonicalSnapshotReader` zentralisiert. Bestätigte aktuelle
`CuratedFieldValues` werden ausschließlich dort priorisiert; danach folgen
relationale Originalwerte und schließlich null.

Die vier fachlichen Internal Data Products konsumieren nur noch Canonical
Snapshots. Bronze- und Silver-Datensätze werden nicht herausgefiltert.
`ImportQualityProduct` liest weiterhin ausschließlich technische Import-,
Issue-, Audit- und Versionsmetadaten.

Quality Level und anwendungsfallspezifische Suitability sind getrennte
Snapshot-Bestandteile. Jahreswerte werden zentral durch
`CanonicalAnnualValue` ermittelt. Unvollständige Jahre liefern
`AnnualValue = null` und den Status `IncompleteYear`; eine Hochrechnung findet
nicht statt.

Offen bleibt die dauerhafte allgemeine Snapshot-Materialisierung für Customer
und EnergySystem. Die bestehende GoldProfileVersion-Persistenz wurde bewusst
nicht durch eine zweite Versionshierarchie dupliziert.

## 21. Analysierte Implementierung

### Reader und Import

- `ExcelImportAnalysisService`
- `ExcelImportReader`, `ExcelWorkbookReader`
- `CsvImportReader`, `CsvMeterReadingReader`
- `LebImportReader`, `LebWorkbookReader`, `LebWorkbookMapper`
- Excel-/CSV-/LEB-Zeilenmodelle und Import-DTO-Mapper
- `ImportCoordinator`, `ExcelImportValidator`, `LebImportValidator`
- `DuplicationCheckService`, `ApplyResolutionService`
- `ImportCommitService`, `ImportWriteGate`, `DatabaseImportWriter`
- `ImportReport`, EF-Entities und `ImportReportPersistenceMapper`

### Domain, Gold und Data Products

- `Customer`, `Building`, `BuildingVersion`, `Meter`, `MeterReading`,
  `ImportedMeterReading`, `EnergySystem`
- `EfCurationService`
- `GoldProfileVersionService`, `DataProductReadinessService`
- `BuildingGoldProfile`, `MeteringPointGoldProfile`
- `BuildingSummaryProduct`, `CustomerSummaryProduct`,
  `MeterSummaryProduct`, `PortfolioSummaryProduct`,
  `ImportQualityProduct`
- `EfInternalDataProductService`
- `NoeLebExportContractV1`, `EfNoeLebContractBuilder`,
  LEB-Mappings und `LebExportValidator`

### REST und UI

- `InternalDataProductsController`
- Import-Response-DTOs und `ImportReportResponseMapper`
- CRUD-ReadDtos und `EfEntityReadService`
- `internalDataProductService`
- `DashboardPage`, `DashboardCockpit`
- `CustomersPage`, `BuildingsPage`, `MetersPage`
- Curation-, GoldProfile- und DataProductReadiness-Panels

## Phase C – vereinheitlichte Lesewege

Customer-, Building- und Meter-Listen sowie deren Detailendpunkte verwenden
`ICanonicalSnapshotReader`. Internal Data Products und Dashboard greifen auf
dieselben kanonischen Werte zurück. Zuordnungen, Quality Level,
Messwertanzahl/-zeitraum und Jahreswert werden nicht mehr in CRUD-Listen
separat abgeleitet.

Relationale EF-Abfragen verbleiben für Write-Vorgänge,
Audit-/Concurrency-Metadaten sowie paginierte Rohmesswerte. Snapshotwerte
werden synchron on demand erzeugt; Phase C führt keine zusätzliche
Snapshot-Persistenz ein.

## Phase D – LEB-Export

Der LEB-Builder liest keine fachlichen EF-Entities oder
`CuratedFieldValues`. Customer-, Building-, Meter-, Messwert- und
EnergySystem-Felder stammen aus `CanonicalSnapshotSet`. Validate, CSV und
Excel verwenden dasselbe `LebExportDataset`.
