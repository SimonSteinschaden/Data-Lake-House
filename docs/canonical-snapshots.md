# Canonical Snapshots

## Zweck

Ein Canonical Snapshot ist die aktuelle fachlich konsolidierte Projektion eines
Customer, Building, Meter oder EnergySystem. Er ist die einzige fachliche Quelle
der Internal Data Products.

`Bronze`, `Silver` und `Gold` sind Qualitätsstufen. Sie sind keine Filter und
entscheiden nicht darüber, ob ein Datensatz generell sichtbar oder verwendbar
ist. Unvollständige Snapshots bleiben deshalb Bestandteil der Products.

## Abgrenzung

- Relationale Tabellen bewahren importierte und operative Daten.
- `CuratedFieldValues` enthalten bestätigte fachliche Korrekturen.
- Der Canonical-Snapshot-Builder führt beide Quellen zusammen.
- Gold bezeichnet das höchste `QualityLevel`, nicht die Snapshot-Schicht.
- `Suitability` bewertet getrennt davon einen konkreten Anwendungsfall.

## Feldpriorität

Für jedes Feld gilt:

1. bestätigter, aktuell gültiger kuratierter Wert;
2. gültiger importierter beziehungsweise relationaler Originalwert;
3. `null`.

Unbestätigte Vorschläge werden nicht als fachliche Wahrheit verwendet.
Unbekannte Werte werden weder ergänzt noch geraten.

## Quality Level

Jeder Snapshot enthält genau ein `SnapshotQuality` mit:

- `Level`: Bronze, Silver oder Gold;
- Vollständigkeit;
- Validität;
- Konsistenz;
- Kurationsgrad.

Internal Products und ihre REST-Repräsentationen übernehmen dieses Ergebnis.
Sie berechnen keine eigene Reife.

## Suitability

`SnapshotSuitability` enthält voneinander unabhängige Bewertungen für LEB,
Navigator, Benchmark und ISO 50001. `Suitable` beziehungsweise `NotSuitable`
wirkt nicht auf das Quality Level zurück. Deshalb sind beispielsweise
`Silver + LEB Suitable` und `Gold + Navigator NotSuitable` zulässig.

## Versionierung

Bestehende aktuelle `GoldProfileVersion`-Metadaten werden als
`CanonicalVersion` weiterverwendet. Release-Status und Quality Level bleiben
getrennt. Existiert für einen Bronze-/Silver-Datensatz noch keine gespeicherte
Profilversion, erhält seine aktuelle relationale Projektion eine deterministische
technische Snapshot-ID und bleibt dennoch sichtbar.

Die dauerhafte Materialisierung allgemeiner Customer- und EnergySystem-Versionen
ist eine spätere Erweiterung; hierfür wurde in Phase B keine zweite
Persistenzhierarchie eingeführt.

## Messwerte und Jahreswerte

Die kanonische Messwertzusammenfassung liefert strukturierte Felder:

- `MeasurementCount`;
- `PeriodStart` und `PeriodEnd`;
- Einheit, ReadingType und Quantity;
- belegtes festes Intervall;
- Quality-Flag-Zählwerte;
- Vollständigkeit;
- `AnnualValue` und `AnnualValueStatus`.

Die zentrale Regel `CanonicalAnnualValue` liefert:

- `NotAvailable`, wenn keine geeigneten Werte existieren;
- `IncompleteYear` und `AnnualValue = null`, wenn nicht alle zwölf Monate eines
  Jahres vertreten sind;
- `CompleteYear` bei zwölf Monaten.

Es gibt keine Hochrechnung. Internal Products aggregieren ausschließlich
`CompleteYear`-Werte.

## Architektur

```text
Import / relationale Persistenz
             |
             v
 bestätigte CuratedFieldValues
             |
             v
   Canonical Snapshot Builder
             |
             v
 ICanonicalSnapshotReader
             |
             v
 Internal Data Products
             |
             v
         REST / UI
```

Direkte fachliche EF-Projektionen und direkte Zugriffe auf
`CuratedFieldValues` sind in den Product Services durch einen Architekturtest
verboten. ImportQuality darf weiterhin technische Import- und Auditdaten lesen.
# Phase-C-Verwendung

CRUD-Listen und -Details für Customer, Building und Meter konsumieren seit
Phase C ebenfalls `ICanonicalSnapshotReader`. Die Snapshots werden aktuell
on demand gelesen; insbesondere wird keine zweite Customer- oder
EnergySystem-Snapshot-Persistenz eingeführt. Details:
[Unified CRUD Read Models](unified-crud-read-models.md).

## Phase D – LEB

Der LEB-Export konsumiert denselben Reader. Die bestehenden Contracts wurden
additiv um Contract-V1-relevante Messwert-, Gemeinde- und Gültigkeitsfelder
ergänzt.
