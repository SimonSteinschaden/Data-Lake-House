# Integration des hierarchischen Qualitätsmodells – Phase 2

Stand: 31.07.2026. Ergänzt eine erste Phase-2-Iteration (Grundgerüst: zentraler
Assessment-Service, Canonical-Snapshot-Anbindung, Invalidierung, Data-Product-
Anbindung, Basis-APIs, Grundgerüst Gebäudedetail) um die verbleibenden
Bausteine bis zur vollständigen Anbindung von Gebäudedetail, Zählpunktdetail,
Datenprüfung, Dashboard, Reports/Exporte, Rollenlogik und Performance-Absicherung.

## Datenfluss

Persistierte Phase-1-Fakten werden batchweise durch
`IHierarchicalQualityAssessmentService` gelesen. Der Service ruft
`HierarchicalQualityAssessment` und `MeterProfileQuality` auf und berechnet
`Warnings`, `NextActions` sowie den `AnnualEnergyStatus` (`NotAvailable` /
`IncompleteYear` / `Estimated` / `CompleteYear` / `Confirmed`) aus den
persistierten Zählpunkt-Jahreswerten (`Meter.AnnualValue`/`AnnualValueOrigin`)
und dem operativen Qualitätslevel der zugehörigen Zählpunkte. Canonical
Snapshots übernehmen ausschließlich diese Ergebnisse; Data Products,
Read-APIs, Objektanalyse, Reports und LEB-Assessment konsumieren wiederum
die Snapshots. `OperationalBuildingQualityAssessment` trägt zusätzlich die
vollständigen `MeterAssessments`/`EnergySystemAssessments` der Kind-Scopes,
damit Gebäudedetail und Reports keine eigene Aggregation vornehmen müssen.
`ICanonicalSnapshotReader` bietet zusätzlich eine batchfähige
`GetEnergySystems`-Methode analog zu `GetBuildings`/`GetMeters`.

## Invalidierung

Gebäudeänderungen heben fachliche Feldbestätigungen und die aktuelle
Inventarerklärung auf. Neue oder geänderte Zählpunkte und Anlagen
invalidieren das betroffene Inventar. Neue oder geänderte Messwerte sowie
Zählpunktstammdaten supersedieren die aktuelle Profilanalyse. Historische
Gold-Versionen und Analysen bleiben unverändert. `EfQualityInvalidationService`
schreibt für jede automatische Invalidierung einen eigenen Audit-Eintrag
(`InventoryAutoInvalidated`, `AnalysisAutoInvalidated`,
`FieldConfirmationsAutoReset`), damit auch systemgetriebene Rückstufungen
nachvollziehbar bleiben.

## APIs und Rollen

Lesezugriffe auf Gebäude und Zählpunkte verwenden den vorhandenen
Mandantenscope. Inventarerklärungen, Analysen, Issues und Entscheidungen
sind zusätzlich durch die `EnsetEmployee`-Policy geschützt. Die
Persistenzschicht prüft finale Schreiboperationen nochmals serverseitig.
Historien- und Issue-Endpunkte (`GetDeclarationHistory`, `GetAnalysisHistory`,
`GetIssues`, `GetDecisionHistory`) sind durchgängig paginiert
(`page`/`pageSize`, Standard 50, maximal 200).

Freigabe und Widerruf von Gold-Profil-Versionen (`POST
/api/v1/gold-profiles/{entityType}/{entityId}/versions/{versionId}/release|revoke`)
erfordern zusätzlich die `EnsetAdmin`-Policy sowie serverseitig die Rolle
`EnsetAdmin` (`GoldProfileVersionService.Change`). Mitarbeitende ohne
Administratorrolle können weiterhin kuratieren, aber keine Gold-Profile
mehr freigeben oder widerrufen. `GoldProfileReleaseStatus` wurde von
`Draft/Released/Superseded/Revoked` auf `Draft/Released/Archived`
konsolidiert (Migration `ConsolidateGoldProfileReleaseStatus`); die
fachliche Unterscheidung zwischen automatischem Ersetzen und manuellem
Widerruf bleibt über `ReleaseReason` und die `GoldProfileEvent`-Historie
nachvollziehbar.

## UI

**Gebäudedetail**: `CurationReadinessPanel` zeigt einen einheitlichen
"Datenqualität"-Bereich mit anklickbarem Status-Badge (Info-Popup mit
Begründung, offenen Anforderungen, untergeordneten Zählpunkten/Anlagen unter
Gold, nächstem Schritt), Fortschrittsanzeige im Format "X % · A von B
Kernanforderungen vollständig", Statusverteilung, den drei Inventar-Ja/Nein-
Zeilen (Zählpunkte/Anlagen vollständig erfasst, keine relevanten Anlagen
bestätigt) sowie deutsch übersetztem Jahresenergie-, Inventar- und
Analysestatus. Die Zählpunkt- und Anlagen-Tabellen in `BuildingsPage.tsx`
zeigen zusätzlich Analysezustand, Vollständigkeit, offene Probleme, letzte
Analyse, fehlende Angaben, Bestätigungsstatus und nächste Aktion je Zeile.

**Zählpunktdetail**: `MeterQualityPanel` zeigt Qualitätsstatus, Analysezustand,
analysierten Zeitraum, Analyseversion, Messintervall, Vollständigkeit,
Lücken- und Anomalienanzahl, blockierende Probleme, Warnungen, letzte Analyse,
bestätigende Person und nächste Aktion. Mitarbeitende können darüber eine
Analyse starten beziehungsweise erneut starten, offene Probleme direkt
einsehen und in die Datenprüfung wechseln.

**Datenprüfung**: `CurationCenterPage` (Feld-Kurationsvorschläge,
`CurationTask`) bleibt unverändert bestehen. Neu: `MeterIssueReviewPage`
(`/tools/data-review/meter-issues?meterId=…`) bindet die neun
`ProfileDecisionType`-Aktionen (bestätigen, korrigieren, als ungültig
markieren, Lücke akzeptieren, Ersatzwert erzeugen, zur Beobachtung
markieren, ignorieren mit Begründung, wieder öffnen) an
`MeterProfileIssue`/`MeterProfileCurationDecision` an, inklusive
Entscheidungshistorie und Pflichtbegründung. `DataQualityWarningsPage`
verlinkt betroffene Zählpunkte direkt dorthin.

**Dashboard**: Das Dashboard berechnet weiterhin keine eigene Qualitätslogik.
Neue Kacheln (Zähler ohne Analyse, Profile mit offenen Blockern/Anomalien,
ungültige Inventarerklärungen, offene fachliche Prüfungen, Gold-Fortschritt
im Portfolio) stammen aus additiven `PortfolioSummaryProduct`-Feldern, die
`EfInternalDataProductService` aus den bereits zentral berechneten
Assessments ableitet.

**Reports und Exporte**: `ReportInstance` friert beim Erstellen zusätzlich
Gold-Fortschritt, Bronze-/Silver-/Gold-Verteilung, Inventarstatus,
Analyseversionen, offene Issues, Blocking Reasons und Bestätigungsstatus des
Gebäudes ein. `IReportService` unterstützt `Release`/`Archive`
(`EnsetAdmin`-Policy); der Kern-Exportcontract (`ObjectAnalyticsProduct`,
LEB-V1) bleibt unverändert. LEB-, CSV-, XLSX- und JSON-Exporte beziehen
Qualitätsdaten weiterhin ausschließlich über `ICanonicalSnapshotReader`.

## Performance

Assessment-Abfragen arbeiten je Scope als Batch:

- eine Projektion für Stammdaten,
- eine Abfrage für aktuelle Analysen,
- eine aggregierte Issue-Abfrage (inklusive Anomalie-Kategorien),
- eine Abfrage für Bestätigungen beziehungsweise Inventarerklärungen.

`AssessMeters`, `AssessEnergySystems` und `AssessBuildings` sind durch
`HierarchicalQualityAssessmentQueryBudgetTests` gegen Einzel-Item-Schleifen
abgesichert (Source-Scan, kein `foreach`, ausschließlich `Where(x =>
set.Contains(...))`-Batchzugriffe). Es werden für Übersichten keine
vollständigen Messwertreihen geladen.

**Bekanntes, weiterhin bestehendes Skalierungsrisiko**: `GetPortfolio`
(`EfCanonicalSnapshotReader`) lädt alle scope-berechtigten IDs ohne
Paging/Batching, und `EfInternalDataProductService` bildet "Gebäude ohne
Zähler" über eine verschachtelte In-Memory-Prüfung (O(Gebäude × Zähler)).
Dies wurde in dieser Iteration nicht behoben (kein Auftrag dafür), sondern
bleibt dokumentiert als Punkt für eine künftige Iteration.

## Abgrenzung

Qualität und Datenproduktfreigabe bleiben getrennt. Es werden keine
berechneten Qualitätswerte persistiert und keine LEB-V1-Vertragsspalten
verändert.

## Bekannte Einschränkungen

- Keine EnergySystem-Feldbestätigung im Kurationsfluss verdrahtet: `AssessEnergySystems`
  liest `CuratedFieldValues` mit `EntityType == "EnergySystem"`, es gibt aber
  aktuell keinen produktiven Schreibpfad, der solche Werte bestätigt — Anlagen
  können dadurch in der Praxis kaum Gold erreichen.
- `ResolvedIssueCount` wird in Reports bewusst nicht eingefroren, da keine
  verlässliche, bereits zentral berechnete Quelle ohne zusätzliche
  Direktabfrage existiert.
- Frontend-Rollenprüfung existiert nicht (kein Client-seitiger
  Auth-/Rollenkontext im gesamten Frontend) — alle Aktionsflächen sind
  sichtbar, serverseitige Policies weisen unautorisierte Aufrufe mit einer
  Fehlermeldung ab.
- Der Portfolio-Reader und die O(n·m)-Schleife für "Gebäude ohne Zähler"
  bleiben ein bekanntes Skalierungsrisiko (siehe Abschnitt Performance).
