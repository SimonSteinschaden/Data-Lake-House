# Architecture Review 1.0 RC

Datum: 20. Juli 2026  
Status: Architecture Freeze Candidate  
Referenz: `ARCHITECTURE_BASELINE_V1_0_RC.md`

## Executive Summary

Der geschätzte MVP-Fertigstellungsgrad beträgt **82 %**. Die Kernschichten sind aufgebaut, kompilieren und sind durch 19 xUnit-Tests sowie Frontend-Lint/Build abgesichert. Importanalyse und Data-Product-Generierung besitzen funktionsfähige Application-Flows. Der größte produktive Blocker ist der nicht implementierte relationale Import-Writer; außerdem fehlen reale Authentifizierung und ein PostgreSQL-basierter End-to-End-Test.

## Reifegrad

| Bereich | Fertig | Bewertung |
|---|---:|---|
| Enset.Domain | 90 % | Kernentities vorhanden; mehrere spätere Domains nur Gerüst |
| Enset.Application | 78 % | Import- und DataProduct-Use-Cases vorhanden; einzelne Validatoren/Platzhalter offen |
| Enset.Infrastructure | 76 % | EF, Migrationen, Reader, Importadapter vorhanden; DatabaseImportWriter fehlt |
| Enset.Api | 78 % | Import- und DataProduct-API implementiert; Auth/zentraler Fehlerkatalog offen |
| Enset.Worker | 42 % | Analyseflow vorhanden; Development-Pfad fest verdrahtet, kein Produktionshost |
| Enset.Web | 68 % | Import und DataProduct-MVP vorhanden; mehrere Seiten nur Gerüst, keine UI-Tests |
| Data Product Engine | 95 % | zwei MVP-Produkte, Reader, Runs, Versionen, API und Dashboard vorhanden |
| Import Engine | 73 % | Analyse/Resolution/Gate/Reports vorhanden; relationaler Commit fehlt |

## Fertige Bereiche

- Azyklische Clean-Architecture-Projektreferenzen
- PostgreSQL-DbContext, explizite EF-Konfigurationen und Migrationen
- Kernmodelle für Customer, Building, Geography, EnergySystem, Meter und MeterReading
- Importanalyse mit Issues, Decision, Resolution, Gate, Audit und Report-Persistenz
- DataProduct-Reader, zwei Generatoren, Authorization, Availability, GenerationRun und Versionierung
- Versionierte REST-Endpunkte mit DTOs und ProblemDetails
- React Import Wizard und DataProduct-Dashboard mit de-AT-Darstellung

## Teilweise fertige Bereiche

- Importpersistenz: Reports sind relational, fachliche Importdaten werden vom DatabaseImportWriter noch nicht geschrieben.
- Data Lake: relationale Fachschicht und Filesystem Raw Zone existieren; Silver/Gold sind noch konzeptionell.
- Authorization: CustomerAssignment wird geprüft; Identity, Claims, Rollen und Mandanten fehlen.
- Web: Data Products sind funktional angebunden, andere Fachseiten sind Platzhalter.
- Worker: ImportCoordinator ist nutzbar, Start/Configuration sind nur für Entwicklung geeignet.

## Architekturprüfung

### Clean Architecture und Zyklen

Die Projektgraphen sind korrekt: Domain hat keine Projektreferenz, Application referenziert Domain, Infrastructure referenziert Application/Domain, Hosts referenzieren Application/Infrastructure. Es wurde kein Projektzyklus gefunden. Controller enthalten Mapping und HTTP-Fehlerübersetzung, aber keine Berechnungslogik. Generatoren verwenden keine EF- oder Dateizugriffe.

### Abweichungen und technische Schulden

1. `DatabaseImportWriter.WriteAsync` wirft `NotSupportedException`; die Dokumentation darf keinen funktionsfähigen relationalen Import behaupten.
2. `Enset.Worker/Program.cs` enthält einen maschinenspezifischen absoluten Excel-Pfad.
3. `BuildingDuplicateValidator` und `MeterDuplicateValidator` werfen `NotImplementedException`; mehrere DuplicationCheck-Dateien enthalten nur TODOs.
4. `GenerateDataProductRequest`, `DataProductGenerationResult`, `DataProductGenerationAvailabilityRequest` und `IDataProductValidator` sind derzeit ungenutzt.
5. `DataProductMarketplacePublication` besitzt noch keinen vollständigen End-to-End-Persistenz-/API-Flow.
6. `IExcelReader`/`IExcelWorkbookReader` und zwei Excel-Writer bilden überlappende Verträge mit unterschiedlichem Zweck und unzureichend klarer Benennung.
7. Mehrere ältere Enums liegen in globalen Namespaces oder ihre Datei-/Namespace-Namen weichen ab; dies kompiliert, erschwert jedoch Navigation.
8. Authorization wird über `X-User-Id` und CustomerId angestoßen; eine authentifizierte Identität fehlt.
9. Der Availability-Service führt im Generation-Flow die Authorization nach der expliziten Authorization erneut aus. Funktional konsistent, aber redundant.
10. Development-Seeding migriert die Datenbank beim API-Start; für produktive Umgebungen ist ein separates Deploymentverfahren nötig.

### Unbenutzte oder vorbereitete Bereiche

Mobility, Subscriptions, Marketplace, Aggregation und Teile von Analytics/EnergyCommunities sind plausible spätere Domains, aber kein abgeschlossenes MVP-Feature. Sie bleiben als Roadmap-Bestand klassifiziert und werden nicht als produktiv dokumentiert.

## Risiken

- **Hoch:** Kein relationaler Import-Commit; der zentrale Data-Lake-Ingestion-Pfad endet vor den fachlichen Tabellen.
- **Hoch:** Keine produktive Authentifizierung/Autorisierung.
- **Mittel:** Versionsnummern besitzen einen Unique Index, aber keine explizite Concurrency-Retry-Strategie.
- **Mittel:** Kein realer PostgreSQL-/HTTP-/Browser-End-to-End-Test im aktuellen Nachweis.
- **Mittel:** Haupt-/Unterzähler können bei Gebäudeaggregation doppelt gezählt werden.
- **Niedrig:** Platzhaltertypen und Benennungsinkonsistenzen erhöhen Wartungskosten.

## Empfehlungen und nächste Schritte

1. P0-Punkte aus `IMPLEMENTATION_ROADMAP_V1_0_RC.md` schließen.
2. PostgreSQL-Testcontainer und vollständigen Smoke-Test in CI etablieren.
3. DatabaseImportWriter als transaktionalen Vertical Slice für Customer/Building/Meter/MeterReading implementieren.
4. Identity-/Claims-Konzept vor externer Bereitstellung verbindlich entscheiden.
5. Erst danach den RC als 1.0-Baseline freigeben.

## Bekannte MVP-Einschränkungen

- Kein produktiver Workerbetrieb oder Scheduling.
- Keine echte Mandantenfähigkeit.
- Keine Meterhierarchie.
- Keine physisch getrennten Silver-/Gold-Zonen.
- Keine React-Komponententests.
- Customer-, Building- und Analytics-Seiten sind nicht fachlich vollständig.

## Freeze-Entscheidung

Die Architektur ist als **1.0 RC dokumentationsreif**, aber noch nicht als produktionsreifes 1.0 MVP freizugeben. Featureentwicklung soll auf dieser Baseline aufsetzen; Die Schichten Domain, Application und Infrastructure gelten mit Ausnahme der definierten P0-Punkte als architektonisch eingefroren. Neue Funktionen sollen bevorzugt durch Erweiterung bestehender Generatoren, Reader und Business-Module erfolgen, ohne die Kernarchitektur zu verändern. P0-Fixes dürfen die Schichtgrenzen nicht verändern.



## Architecture Decision

- ADR-001:Data Products entstehen ausschließlich aus dem Data Lake.Nicht aus Uploads.
- ADR-002:Generatoren verwenden ausschließlich Reader. Keine EF-Abfragen. Keine EF-Abfragen.
- ADR-003:Application enthält Berechnungslogik. Domain enthält keine Berechnungen.
- ADR-004:Versionierung aller Data Products. Keine Überschreibung.
- ADR-005:GenerationRun protokolliert jede erfolgreiche Berechnung.
- ADR-006:Reader abstrahieren Datenquellen. Generatoren kennen keine Speichertechnologie.

























































