# ENSET Datenkuration

Die Curation-Schicht liegt zwischen CRUD/Import und Data Products. Sie verändert
keine Originalwerte. Fachliche Vorschläge und bestätigte Werte werden separat,
feldbezogen und mit Herkunft gespeichert.

## Datenreife

- **Bronze:** vorhandener Roh- oder Importwert ohne ausreichende technische Prüfung.
- **Silver:** technisch verwendbarer, strukturell eindeutiger Wert.
- **Gold:** fachlich bestätigter kuratierter Wert.

Der aggregierte Reifegrad wird aus den Pflichtfeldern eines Profils abgeleitet.
Er kann nicht über einen Request-Body gesetzt werden. Ein teilweise kuratiertes
Objekt kann daher gleichzeitig Bronze-, Silver- und Gold-Felder besitzen.

## Vorschläge und Herkunft

Regeln sind deterministisch versioniert. Jeder Vorschlag enthält `RuleId`,
`RuleVersion`, Konfidenz und eine verständliche Begründung. Annahme, Anpassung
und Ablehnung werden als Entscheidung protokolliert. Angepasste Werte erhalten
die Quelle `User` und 100 Prozent Konfidenz. Frühere Feldwerte bleiben über
`ValidFromUtc` und `ValidToUtc` nachvollziehbar.

## Gold-Profile

`BuildingGoldProfile` und `MeteringPointGoldProfile` sind abgeleitete interne
Sichten, keine Kopien der Stammdaten. Das Zählpunktprofil berechnet in UTC
Messzeitraum, erwartete und tatsächliche Intervalle, Lücken, ungültige,
geschätzte und interpolierte Werte sowie gemessene und abgeleitete Anteile.

Die Profile halten Kategorien von direkt identifizierenden Stammdaten getrennt.
Eine spätere pseudonymisierte Data-Product-Grenze kann interne IDs ersetzen und
Postleitzahlen abhängig von der Gruppengröße vergröbern. Namen, externe
Kundennummern und exakte Adressen gehören nicht in Benchmark-Datensätze.

## API

Unter `/api/v1/curation` stehen Statistik, paginierte und filterbare Aufgaben,
Entscheidungen, Building-/MeteringPoint-Profile, Readiness und objektbezogene
Regelauswertung zur Verfügung. Die Endpunkte verwenden die bestehende
Authentifizierung, Sichtbarkeit und RFC7807-Fehlerbehandlung.

## Abgrenzung und Einschränkungen

Curation ist weder Import noch CRUD. Es gibt keine KI, globale Massenevaluierung,
automatische Fachfreigabe, Dublettenbereinigung oder Geocodierung. Benchmarking,
Lastprofil-Matching, EEG/P2P und Data Products V2 sind nicht implementiert.
