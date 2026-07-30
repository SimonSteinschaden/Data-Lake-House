# ADR: Staging-basierter MeterReading-Import

## Kontext

MeterReading-Dateien sind um Größenordnungen größer als Stammdatenimporte.
EF `AddRange` reduziert zwar Roundtrips, hält aber weiterhin sämtliche
Entities im ChangeTracker; ein synchroner Request und feldweises Audit
skalieren deshalb nicht.

## Entscheidung

CSV-Datensätze werden begrenzt in Chunks gelesen, mittels PostgreSQL Binary
COPY in eine technische Staging-Tabelle geschrieben, dort mengenorientiert
validiert und mit `INSERT ... ON CONFLICT` übernommen. Der Commit startet
einen internen Background Job mit persistiertem Fortschritt. MeterReadings
werden aus dem generischen Entity-Audit ausgeschlossen und je Import/Zähler
aggregiert auditiert.

## Alternativen

- EF `AddRange`: verworfen wegen ChangeTracker- und Audit-Wachstum.
- Direktes COPY in die Zieltabelle: verworfen, da Validierung,
  Deduplizierung und Diagnose vor dem fachlichen Write fehlen.
- Externer Broker: für dieses Arbeitspaket bewusst außerhalb des Scopes.
- Eine Transaktion über die gesamte Datei: verworfen wegen langer Locks und
  schlechter Abbruchbarkeit.

## Konsequenzen

Staging benötigt Migration, Indizes und Retention. Fortschritt ist nach
DbContext-Wechsel lesbar; ein Neustart hinterlässt `Interrupted`, nimmt Jobs
aber noch nicht wieder auf. Chunkgrenzen begrenzen Speicher und
Transaktionsdauer. Später sind verteiltes Leasing, Resume ab bestätigtem
Batch, partitioniertes Staging und erweiterte SQL-Regelkataloge möglich,
ohne Canonical Snapshots oder eine zweite fachliche Persistenz einzuführen.
