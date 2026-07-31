# MeterReading-Massenimport

## Architektur

Der bisherige CSV-Commit materialisierte alle Messwerte im `ImportReport`,
verfolgte sie als EF-Entities und schrieb sie in einem synchronen
HTTP-Vorgang. Dadurch wuchsen Heap, ChangeTracker und feldweise
`EntityAuditHistory` proportional zur Dateigröße.

Der Datenpfad ist nun:

`CSV-Stream → 10.000er Chunks → Npgsql Binary COPY → technische
ImportStagingMeterReadings → SQL-Validierung/Deduplizierung → INSERT ... ON
CONFLICT → MeterReadings`.

Die Chunkgröße und Retention werden unter `MeterReadingMassImport`
konfiguriert. Der Reader liest Header und Datensätze sequenziell, reicht
Cancellation weiter und hält nur einen Chunk. Zeilennummer und SHA-256 des
Rohdatensatzes bleiben für Diagnosen erhalten.

## Staging, Validierung und Deduplizierung

`ImportStagingMeterReadings` ist ausschließlich eine technische,
kurzlebige Arbeitsstruktur und keine zweite fachliche Persistenz. Indizes
decken Import, Batch, Status sowie `(MeterId, Timestamp)` ab.

Gebündelte SQL-Schritte lösen den Zähler auf und prüfen Pflichtwerte.
Dateiinterne Dubletten werden mit `ROW_NUMBER()` über
`(MeterId, Timestamp)` markiert. Zielkonflikte werden als
`EXISTING_IDENTICAL` oder `EXISTING_CONFLICT` klassifiziert. Der bestehende
fachliche Unique Key der Zieltabelle bleibt `(MeterId, Timestamp)`.

Die vorhandenen Modi `Upsert` und `Replace` aktualisieren bei einem Konflikt
kontrolliert Wert, Typ, Qualität, Intervall und Importherkunft. Es wurde kein
neuer Insert-/Skip-Modus erfunden.

## Job, Fortschritt und Abbruch

`POST /api/v1/imports/{id}/commit` liefert für CSV-Datenbankimporte `202`
mit Job- und Status-URL. Ein interner, einzelner Background-Worker verarbeitet
die persistierte Jobbeschreibung. Status sind `Queued`, `Reading`, `Staging`,
`Validating`, `Writing`, `Completed`, `Failed`, `Cancelled` und
`Interrupted`. Nach einem API-Neustart werden nichtterminale Jobs
reproduzierbar als `Interrupted` markiert.

Fortschritt wird je Chunk persistiert. Cancellation beendet den Reader oder
Binary-COPY-Vorgang; die jeweilige Chunk-Transaktion wird zurückgerollt.
Bereits vollständig abgeschlossene finale SQL-Schreibvorgänge bleiben
erhalten. Exceptions verlassen den Worker nicht und werden als Jobfehler
gespeichert.

Das generische feldweise Audit ignoriert `MeterReading` vollständig.
`MeterReadingImportAudits` enthält stattdessen eine Zusammenfassung je
Import und Zähler mit Zeitbereich und Read/Written/Rejected/Duplicate Counts.

## Transaktionen und Retention

Jeder Staging-Chunk besitzt eine kurze eigene Transaktion. Validierung und
finale Übernahme sind getrennt; Statusupdates liegen außerhalb langer
Transaktionen. Vor Jobbeginn entfernt ein idempotenter Cleanup Staging-Daten
erfolgreicher Jobs nach standardmäßig 7 Tagen und fehlgeschlagener,
abgebrochener oder unterbrochener Jobs nach 30 Tagen.

## Validated Performance

Die isolierten PostgreSQL-18.4-Läufe validierten 35.040, 1.000.000 und
3.000.000 Zeilen. Der vollständige Commit benötigte 2,39 s, 48,36 s und
180,58 s. Peak Working Set blieb zwischen 121,8 und 140,1 MiB; der
EF-ChangeTracker enthielt maximal zwei Einträge. Details und SQL-Pläne stehen
in [METER_READING_BENCHMARK.md](METER_READING_BENCHMARK.md).

Die interaktive CSV-Analyse verwendet nun denselben begrenzt speichernden
Reader. Der `ImportReport` enthält Aggregate, Header, Issues und höchstens
20 Beispielzeilen, nicht mehr sämtliche gültigen Messwerte.

## Benchmarks und bekannte Einschränkungen

Deterministische Dateien werden mit
`tools/generate-meter-reading-csv.ps1` erzeugt und nicht eingecheckt.
Zu erfassen sind Dateigröße, Zeilen, Gesamtdauer, Staging-/Validierungs-/
Schreibdauer, Durchsatz, Peak Memory und SQL-Kommandos. Belastbare
PostgreSQL-Laufzeiten sind umgebungsabhängig und werden nicht als
plattformunabhängige Assertions behandelt.

Der Worker ist pro API-Instanz eine In-Process-Queue ohne verteiltes Leasing
oder automatische Wiederaufnahme. Quoted CSV-Felder werden unterstützt,
mehrzeilige quoted Records derzeit nicht. Analyse und Commit sind
speicherbegrenzt; exakte dateiinterne Deduplizierung erfolgt weiterhin in
der SQL-Stagingphase und nicht im Analyseprozess.
