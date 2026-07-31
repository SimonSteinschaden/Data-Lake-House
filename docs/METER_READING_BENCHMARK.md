# MeterReading Benchmark

## Umgebung und Reproduzierbarkeit

Messdatum: 30. Juli 2026. Host: Intel Core i5-12400F, 12 logische
Prozessoren, 31,82 GiB RAM, Windows mit Docker Desktop. Datenbank:
isolierter, kurzlebiger Container `postgres:18`, PostgreSQL 18.4, Port 55432,
Datenbank `enset_benchmark`. Die Entwicklungsdatenbank auf Port 5432 wurde
nicht verwendet.

Die Umgebung wird reproduziert durch:

1. PostgreSQL-Container und leere Datenbank erstellen.
2. Alle EF-Migrationen anwenden.
3. CSV mit `tools/generate-meter-reading-csv.ps1` erzeugen.
4. `tools/Enset.MeterReadingBenchmark` mit Connection String und CSV starten.

Der Runner leert ausschließlich die Benchmarktabellen, legt einen
Benchmarkzähler an, führt Streaming-Analyse und vollständigen Commit aus und
gibt JSON-Telemetrie aus. Chunkgröße: 10.000.

## Ergebnisse

| Zeilen | Dateigröße | Analyse | Commit | Durchsatz | Chunks/COPY | Peak WS | Ø WS | CPU | EF max. |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 35.040 | 3.079.391 B | 0,20 s | 2,39 s | 14.660/s | 4 | 121,8 MiB | 111,7 MiB | 0,83 s | 2 |
| 1.000.000 | 87.880.077 B | 2,68 s | 48,36 s | 20.676/s | 100 | 140,1 MiB | 124,5 MiB | 3,70 s | 2 |
| 3.000.000 | 263.640.077 B | 6,76 s | 180,58 s | 16.613/s | 300 | 139,4 MiB | 127,6 MiB | 8,09 s | 2 |

Alle Läufe schrieben sämtliche Zeilen; Rejects, Datei-Dubletten und
Zielkonflikte waren jeweils null. Die höchste gemessene mittlere
Importgeschwindigkeit betrug 20.676 Zeilen/s. Eine separate instantane
Maximalrate pro Chunk wurde nicht instrumentiert.

Der Peak des 3-Millionen-Laufs liegt unter dem des 1-Millionen-Laufs. Der
EF-Tracker blieb bei zwei Einträgen. Damit wachsen weder Prozessspeicher noch
ChangeTracker linear mit der Zeilenzahl. Container-I/O nach den Läufen:
221 MB gelesen, 70,8 GB geschrieben; der hohe Schreibwert enthält drei
vollständige Läufe, fehlgeschlagene Diagnosewiederholungen, Indexaufbau und
Rollback des EXPLAIN-Laufs. Er ist kein Einzelimportwert.

Pro Chunk entstehen ein COPY-Vorgang und persistierte Fortschrittskommandos.
Die logische Client-Kommandomenge ist damit `3 × Chunks + konstante
Abschlusskommandos`; die Validierung enthält fünf SQL-Statements in einem
Roundtrip. PostgreSQL-seitige Einzelstatementzählung war ohne vorab
aktiviertes `pg_stat_statements` nicht rückwirkend belastbar.

## Gefundene Fehler und Optimierungen

Der erste echte Lauf fand eine falsche Ressourcenreihenfolge: Binary COPY
war bei `COMMIT` noch aktiv. Der Importer wird nun vor dem
Transaktions-Commit freigegeben.

Beim ersten 3-Millionen-Lauf überschritt die Deduplizierung den impliziten
30-Sekunden-Timeout. Ergänzt wurden:

- Index `(ImportId, MeterId, Timestamp, SourceRowNumber)` für die
  `ROW_NUMBER()`-Ordnung;
- konfigurierbares `BulkCommandTimeoutSeconds`, Standard 300 Sekunden;
- dasselbe Timeout für EF-Bulk-SQL und das direkte Npgsql-Zielkommando.

Migration:
`20260730175527_OptimizeMeterReadingStagingDeduplication`.

## EXPLAIN ANALYZE

Die Valid-Statusprüfung verwendet
`IX_ImportStagingMeterReadings_ImportId_ValidationStatus` als Index Only
Scan: 0,090 ms Execution Time, 15 Buffer-Blöcke, keine Heap Fetches.

Die Deduplizierungsprojektion über 3.000.000 Zeilen verwendet den neuen
viergliedrigen Index als Index Only Scan mit anschließendem WindowAgg:

- Execution Time: 2.880,857 ms
- Rows: 3.000.000
- Window-Speicher: maximal 17 kB
- kein expliziter Sort und kein Sequential Scan
- Heap Fetches: 3.000.000
- Buffers: 5.999.994 Hits, 149.596 Reads

Ein `VACUUM (ANALYZE)` nach dem COPY könnte die Visibility Map verbessern
und Heap Fetches reduzieren, verursacht aber zusätzliche Laufzeit und wurde
nicht automatisch in den Importpfad aufgenommen.

Der kontrollierte `ON CONFLICT`-Plan mit 35.040 Zielkonflikten verwendete
den Staging-Statusindex und
`IX_MeterReadings_MeterId_Timestamp` als Conflict Arbiter:

- Execution Time: 1.221,092 ms
- Conflicting Tuples: 35.040
- Inserted Tuples: 0, Updates: 35.040
- kein Sequential Scan

Der künstliche Teilmengentest filterte 2.964.960 Stagingzeilen nachträglich.
Der produktive vollständige Insert benötigt diesen Filter nicht.

## Parallelität und Bottlenecks

Der In-Process-Background-Service besitzt bewusst genau einen Queue-Reader.
Zwei große und mehrere kleine gleichzeitig angenommene Jobs werden pro
API-Instanz seriell verarbeitet. Dadurch wurden keine konkurrierenden
Zielwrites und keine Deadlocks beobachtet; kleine Jobs können hinter einem
großen Job warten. Mehrere API-Instanzen besitzen derzeit weder verteiltes
Leasing noch globale Queue-Synchronisation und sind daher noch keine
freigegebene Parallelimport-Topologie.

Aktuelle Bottlenecks sind PostgreSQL-WAL/Storage beim Zielwrite, das
WindowAgg über die vollständige Importmenge sowie die serielle
Head-of-Line-Queue. Etwa 100 MB und 1 GB sind mit ausreichend freiem
Staging-/WAL-Speicher architektonisch plausibel. 10 GB sind zwar
streamingfähig, aber ohne Partitionierung, Resume, verteiltes Leasing und
produktionsnahe Langzeittests noch nicht als produktiv validiert.
