# Importquelle Landesenergiebuchhaltung

Die Landesenergiebuchhaltung (LEB) ist eine eigenständige Importquelle für
kommunale Gebäude. Der bestehende CRM-Excel-Import bleibt davon fachlich
und technisch getrennt.

## Importfluss

```text
Upload (SourceType + Medium + Datei)
  -> ImportAnalysisService
     -> CRM Excel: bestehender Reader / Mapper / Validator
     -> LEB: LebWorkbookReader
             -> LebWorkbookDto / LebRowDto
             -> LebWorkbookMapper
             -> kanonischer Import-Payload
             -> LebImportValidator
  -> ImportReport
  -> Benutzerentscheidungen
  -> ReadyToCommit
  -> gemeinsamer WriteGate / Writer
```

`ImportSourceType` unterscheidet `CRM_Excel` und
`Landesenergiebuchhaltung`. Für LEB ist `ImportMedium` (`Electricity` oder
`Heat`) verpflichtend. Das Medium wird ausdrücklich vom Benutzer gewählt und
nicht aus den Quelldaten abgeleitet.

Der LEB-Reader unterstützt XLSX/XLSM und CSV mit Semikolon, Komma oder Tab als
Trennzeichen sowie UTF-8, UTF-8 mit BOM und Windows-1252. Leere Zeilen und
wiederholte Header werden ignoriert. Der Reader bildet ausschließlich das
Quellformat ab; fachliche Transformation und Validierung erfolgen danach.

Leere Headerzellen bleiben positionsstabil erhalten und erhalten pro
Headerzeile reproduzierbar die Namen `Tabelle1`, `Tabelle2`, … . Alle
Quellwerte werden zusammen mit Spaltenindex, Originalheader, effektivem Header
und `HasData` im Analysemodell gehalten. Das erste `Jahr` wird als
`ReadingYear`, das zweite als `AnnualTotal` geführt. Ein wiederholter Header
erzeugt daher exakt dasselbe Schema.

Eine automatisch benannte Spalte mit Daten erzeugt ein
`SourceColumnMappingRequired`-Issue. Der Benutzer kann den vorläufigen Namen
bestätigen, einen Namen beziehungsweise ein bekanntes Feld zuordnen oder die
Spalte bewusst als Zusatzspalte übernehmen. Die Entscheidung und ein
benutzerdefinierter Name werden mit dem Importreport gespeichert. Automatisch
benannte Spalten ohne Daten bleiben im Report sichtbar, erzeugen aber kein
blockierendes Issue.

## Wiederverwendbare Gruppenentscheidungen

Gleichartige Issues werden anhand strukturierter Kriterien gruppiert:
Importquelle, Issue-Typ, Feldname und `ImportIssueValuePattern`. Nur wenn kein
strukturiertes Pattern vorhanden ist, wird zusätzlich der normalisierte
Feldwert als exaktes Matchkriterium verwendet. Meldungstexte werden nicht
analysiert.

`POST /api/v1/imports/{importId}/resolution-rules` unterstützt
`SingleIssue` und `MatchingIssuesInCurrentImport`. `FutureImports` ist im
Vertragsmodell vorbereitet, bleibt aber bis zu einem Governance-Konzept
serverseitig deaktiviert.

Eine Regel wird im `ImportReport` zusammen mit Scope, Resolutiontyp, Payload,
Anwender, Zeitpunkt sowie Treffer- und Auflösungszahl gespeichert. Der
Apply-Service überspringt bereits gelöste Issues, schreibt einen aggregierten
Audit-Eintrag, berechnet anschließend zentral die Commitfähigkeit neu und wird
vom Controller mit genau einem Repository-Load und einem Repository-Save
persistiert.

API-Responses liefern bei großen Reports höchstens 500 Issues, enthalten aber
für jede repräsentierte Gruppe die vollständige Trefferzahl sowie
`issueCount`, `returnedIssueCount` und `hasMoreIssues`. Die eigentliche
Gruppenauflösung findet ausschließlich serverseitig statt.

### Gruppierte Zahlenformatfehler

`InvalidNumberFormat` wird nicht anhand des Meldungstexts oder des konkreten
Originalwerts gruppiert. Der Analyzer speichert stattdessen strukturiert den
Zieldatentyp `Decimal` und eines der Muster `AustrianDecimal`,
`InvariantDecimal`, `AmbiguousDecimal` oder `Invalid`. Für Zahlenregeln setzt
sich die Gruppe aus Import, Quelle, Issue-Typ, Zieldatentyp und erkanntem
Muster zusammen. Monats- und Jahreswerte können daher derselben Gruppe
angehören; der Feldname und der jeweilige Zahlenwert trennen sie nicht.

Sichere Muster bieten jeweils nur die passende Batch-Parsingregel an.
Mehrdeutige und tatsächlich ungültige Werte werden nicht automatisch mit
einer Kultur geparst. Beim Anwenden einer Gruppenregel wird jeder Wert einzeln
validiert. Nicht parsebare Werte bleiben mit unverändertem Originalwert offen,
werden in `failedIssueCount` gezählt und verhindern nicht die Verarbeitung der
übrigen Gruppenelemente. Treffer-, Erfolgs- und Fehlerzahl, Auditdaten sowie
die neu berechnete Commit-Readiness werden gemeinsam mit dem Report
persistiert.

### IssueType-weite Resolution

Neben `SingleIssue` und der aktuell dargestellten Gruppe unterstützt eine
Resolution-Regel den Scope `MatchingIssueTypeInCurrentImport`. Dieser Scope
ignoriert den konkreten Originalwert, bleibt aber auf fachlich kompatible
Issues begrenzt. Der Kompatibilitätsschlüssel enthält Issue-Typ, Feldkontext,
Zieldatentyp, Value- und Zahlenmuster sowie die vollständige Menge erlaubter
Resolutionen. Unterschiedliche Referenzkontexte oder Resolution-Matrizen
werden dadurch nicht vermischt.

Das Matching findet ausschließlich im geladenen ImportReport statt. Die Regel
speichert Scope, Matchkriterien, Aktion, Anwender, Zeitpunkt sowie Treffer-,
Erfolgs-, Fehler- und Überspringungszahlen. Der Client sendet pro
Gruppenentscheidung einen Request und erhält den aktualisierten Report mit
neu berechneter Readiness und repräsentativen Restgruppen zurück.

## Fehlende Daten und Resolution-Matrix

Fehlende fachliche Werte wie `AnnualTotal`, `m2` oder `Baujahr` bleiben leer
und werden als nicht blockierende `MissingData`-Hinweise mit Severity
`Warning`, Quellzeile und Feldname gespeichert. Sie benötigen keine
Benutzerentscheidung. Optional kann ein Benutzer einen typvalidierten Wert
eingeben, den Mangel bewusst akzeptieren oder bei numerischen Feldern explizit
`0` setzen. `NULL` und `0` bleiben dadurch auditierbar verschieden.

Die erlaubten Aktionen stammen ausschließlich aus dem serverseitigen
`ImportResolutionOptionsProvider`. Duplikate erhalten Vergleichsaktionen,
Zahlenfehler numerische Aktionen, Referenzprobleme Referenzaktionen und
generierte Header Mappingaktionen. Nicht-Duplikate liefern keinen
`SecondValue`. Strukturfehler bieten keine interaktive Resolution und bleiben
blockierend.

Monatswerte werden jeweils auf den ersten Tag des Monats in UTC abgebildet.
Leere Monatswerte erzeugen keinen Messwert.

## Fachliche Identität und Domänentrennung

LEB-Gebäude werden ausschließlich über die externe Identität `(GemID, GebID)`
wiedererkannt. Technisch werden quellennamensräumige Schlüssel verwendet:

```text
Organisation/Gemeinde: LEB:GEM:{GemID}
Gebäude:                LEB:GEM:{GemID}:GEB:{GebID}
Zähler:                 LEB:GEM:{GemID}:GEB:{GebID}:Z:{ZId}
```

Damit kann ein neues LEB-Gebäude nicht automatisch mit einem klassischen
ENSET-Gebäude kollidieren. Ortsname, Gebäudebezeichnung, Baujahr und Fläche
werden niemals als Matchingkriterien verwendet. Eine spätere Zusammenführung
darf nur durch eine explizite Benutzerentscheidung oder eine bereits
gespeicherte externe Referenz erfolgen.

Im aktuellen kanonischen Modell wird die Gemeinde kompatibel als `Customer`
transportiert. Die langfristige Domänengrenze ist jedoch `Organization`:

```text
Organization
├── Municipality
├── Company
├── PrivateOwner
├── HousingAssociation
└── EnergyCommunity
```

Jedes `Building` soll langfristig genau einer `Organization` gehören.
Kundensegmentspezifische Importquellen, Datenmodelle und Oberflächen bleiben
außen getrennt und verwenden intern weiterhin das gemeinsame kanonische
Gebäude-, Zähler- und Messwertmodell. Die Einführung dieser Domänenmigration
ist bewusst nicht Teil des LEB-Imports, weil sie bestehende
Customer-Autorisierung und Persistenzverträge separat migrieren muss.
