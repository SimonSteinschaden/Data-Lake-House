# LEB-Export – NoeLebExportContractV1

## Ziel

`NoeLebExportContractV1` ist das erste offizielle External Data Product von
ENSET. Es ist ein versionierter, standardisierter Behördenexport für die
niederösterreichische Landesenergiebuchhaltung und ausdrücklich kein
Marketplace-Produkt.

## Architektur

`Gold Profiles -> Internal Data Products -> NoeLebExportContractV1 ->
Validation -> CSV / Excel`

Der read-only Contract Builder verwendet den bestehenden tenantabhängigen
DataAccessScope. Der Zeitpunkt des Portfolio-Snapshots stammt aus dem
`PortfolioSummaryProduct`; die für den vollständigen Vertrag benötigten
Stammdaten und Messwerte werden aus bestehenden Domain-/ReadModels ergänzt.
Controller- und Frontendmodelle sind keine Datenquelle. Es gibt keine neue
Persistenz, Domain Entity oder Migration.

## Exportvertrag und Tabellen

Der Vertrag trägt den Namen `NoeLebExportContractV1` und die Version `1.0`.
Er enthält fünf logisch eigenständige Tabellen:

- `Municipalities`: Gemeinde-ID, Gemeindenummer, Name, Hauptregion,
  Exportzeitpunkt.
- `Objects`: Objektklassifikation, Nutzung, Adresse, Bau- und Flächendaten,
  Referenzgröße und Kontakt.
- `Meters`: Objektzuordnung, Zähler-/Zählpunktdaten, LEB-Medium, Richtung,
  Messwertart, Einheit und Gültigkeit.
- `Readings`: Zeitstempel, Wert, Einheit, Art, Qualität, Quelle und
  Berechnungskennzeichen.
- `EnergySystems`: Objektzuordnung, Versorgungszweck, Energieträger, Leistung
  und Gültigkeit.

Nicht im Datenmodell vorhandene Angaben bleiben `null`. Beispielsweise werden
unkonditionierte Flächen und Volumina oder Anlagenbaujahre nicht erfunden.

## Zentrale Mappings

- `NoeEnergyCarrierMapper`
- `NoeBuildingUsageMapper`
- `NoeMeterCategoryMapper`
- `NoeMeasurementDirectionMapper`
- `NoeReadingTypeMapper`

Unbekannte Werte liefern `null` und lösen gegebenenfalls einen
Validierungsfehler aus. Es gibt keine verteilten LEB-Mapping-Switches in
Controllern oder Exportern.

## Validierung

Vor jedem CSV- oder Excel-Export validiert `LebExportValidator` denselben
vollständigen Vertrag. Blockierend sind:

- fehlende Gemeinde/Gemeindenummer, Objektcode, Nutzung oder konditionierte
  Fläche;
- Zähler ohne Objekt, Medium, Einheit oder Kategorie;
- unbekanntes Navigator-Medium;
- Messwerte ohne Zeitstempel oder Wert.

Warnungen verhindern den Export nicht:

- fehlendes Baujahr, Geschoßanzahl, Volumen, Ansprechpartner,
  Zählpunktnummer oder Referenzgröße;
- weniger als zwölf unterschiedliche Monate mit Messwerten;
- fehlende PV- oder Heizleistung.

`ValidationResult.CanExport` ist nur dann `true`, wenn keine blockierenden
Fehler bestehen. Fehler und Warnungen enthalten Code, Tabelle, Zeilenreferenz,
Feld und verständlichen Text.

## CSV und Excel

Der CSV-Endpunkt liefert ein ZIP mit:

- `Municipalities.csv`
- `Objects.csv`
- `Meters.csv`
- `Readings.csv`
- `EnergySystems.csv`

CSV wird als UTF-8 mit BOM, Semikolon, ISO-8601-Zeitstempeln und
kulturunabhängigem Dezimalpunkt geschrieben. Das XLSX enthält entsprechend die
Arbeitsblätter `Municipalities`, `Objects`, `Meters`, `Readings` und
`EnergySystems`, jeweils mit einfacher Kopfzeile und Tabelle.

## API

Alle Endpunkte liegen unter `/api/v1/exports/leb`, verwenden die bestehende
`CustomerReader`-Policy und akzeptieren optional `CustomerId`, `ReadingFrom`
und `ReadingTo` im JSON-Body.

- `POST /validate`: ausschließlich Contract-Aufbau und Validierung.
- `POST /csv`: validiert und liefert ZIP; bei Fehlern HTTP 422.
- `POST /excel`: validiert und liefert XLSX; bei Fehlern HTTP 422.

Ein ungültiger Zeitraum liefert RFC-7807-ProblemDetails mit HTTP 400.

## Benutzeroberfläche

Die Hauptnavigation enthält direkt unter `Importe` den Eintrag `Exporte`.
Die zugehörige Seite unter `/exports` stellt den CSV- und Excel-Export als
separate Karten dar und zeigt außerdem die vorgesehenen, noch deaktivierten
Exportformate.

Beide LEB-Karten verwenden `POST /api/v1/exports/leb/validate` zur
Validierung desselben `NoeLebExportContractV1`. Noch nicht geprüfte Exporte
werden entsprechend gekennzeichnet. Warnungen werden gelb dargestellt und
lassen den Export weiterhin zu. Blockierende Fehler werden rot mit Tabelle
und betroffenem Feld ausgegeben und deaktivieren beide Download-Aktionen.

`CSV herunterladen` ruft `POST /api/v1/exports/leb/csv` auf und startet den
Browser-Download des ZIP-Archivs. `Excel herunterladen` verwendet
`POST /api/v1/exports/leb/excel` und lädt die XLSX-Arbeitsmappe direkt
herunter. Es gibt keine Zwischenseite. Da beide Download-Endpunkte erneut
serverseitig validieren, wird ein dabei zurückgegebenes HTTP 422 ebenfalls als
blockierendes Validierungsergebnis auf der Exportseite dargestellt.

## Bekannte Einschränkungen

- Kein Siemens-Navigator-Connector, Upload oder automatische Synchronisierung.
- Keine LEB-spezifische Berechnung, Hochrechnung oder Zeitreihenanalyse.
- Grid-Metering-Point wird nur aus der vorhandenen externen Zählerreferenz
  übernommen.
- Fehlende LEB-Felder bleiben transparent leer.
- Die genaue behördliche Schemaabnahme und ein späterer Navigator-Transport
  sind nachfolgende Arbeitspakete.

## Phase D – kanonische Exportquelle

Der Contract Builder verwendet ausschließlich `ICanonicalSnapshotReader` und
erzeugt ein `LebExportDataset`, das Validate, CSV und Excel gemeinsam
verwenden. Quality Level und LEB Suitability sind getrennt; Bronze und Silver
werden nicht pauschal ausgeschlossen. Details:
[LEB Export – Canonical Projection](leb-export-canonical-projection.md).
