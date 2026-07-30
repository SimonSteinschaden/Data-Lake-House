# LEB Export – Canonical Projection

## Ziel und Datenfluss

```text
ICanonicalSnapshotReader
  -> LebExportDataset
  -> NoeLebExportContractV1
  -> LebExportValidator
  -> CSV oder Excel
```

`EfNoeLebContractBuilder` ist die einzige fachliche Exportprojektion.
Validate, CSV und Excel laden über diesen Builder denselben Dataset-Typ. CSV
und Excel sind ausschließlich unterschiedliche Serialisierungen desselben
Contract-V1-Objekts.

## Kanonische Quellen und Mapping

| Contracttabelle | Kanonische Quelle |
|---|---|
| Municipalities | Building-Gemeinde, Gemeindenummer, Name, Hauptregion |
| Objects | Building-Stammdaten, Adresse, Nutzung, Bau- und Flächendaten; Customer-Kontakt |
| Meters | originale MeterNumber, getrenntes Name, Typ, Medium, Richtung, Einheit |
| Readings | CanonicalMeterReading |
| EnergySystems | EnergySystemCanonicalSnapshot |

Kuratierte Werte werden ausschließlich beim Snapshotaufbau priorisiert. Der
Export kennt `CuratedFieldValues` nicht. Technische GUIDs stehen nur in den
vorhandenen ID-Spalten und dienen nie als fachlicher Fallback. Fehlende Werte
bleiben leer.

## Quality Level und LEB Suitability

`LebExportAssessment` hält pro Building und Meter Quality Level und die davon
unabhängige LEB Suitability fest. Bronze, Silver und Gold werden gleichermaßen
aufgenommen. `NotSuitable` erzeugt den blockierenden Fehler
`LEB_NOT_SUITABLE`; Quality Level allein blockiert nie.

Contract V1 besitzt keine entsprechenden Spalten und wurde deshalb nicht
erweitert. Das Validate-Ergebnis liefert additiv Dataset-, Suitable-,
NotSuitable-, Warning- und Blocking-Error-Anzahlen.

## Jahreswerte und Messwerte

Der Export berechnet und extrapoliert keine Jahreswerte. Die zentrale Semantik
bleibt in `CanonicalReadingSummary` (`CompleteYear`, `IncompleteYear`,
`NotAvailable`). Einzelwerte kommen aus `CanonicalMeterReading`.
Unvollständige Zeiträume werden transparent validiert und nicht hochgerechnet.

## Version, technische Metadaten und EF

`ExportTimestamp` wird einmal beim Dataset-Aufbau erzeugt.
`SnapshotCreatedAt` ist der jüngste Versionszeitpunkt der beteiligten
Building-/Meter-Snapshots. Es gibt keine künstliche globale Snapshotversion.

Im LEB-Exportnamespace verbleiben keine direkten EF-Abfragen – auch keine
technischen, weil derzeit keine ExportJob-Persistenz benötigt wird.

## Fehlerverhalten und Einschränkungen

Blockierende Contractfehler oder `NotSuitable` verhindern beide Formate und
führen über das bestehende API-Muster zu HTTP 422. Warnungen erlauben den
Export.

Portfolio-N+1, `includeDeleted` und NU1903 bleiben als ausdrücklich
ausgeschlossene Themen unverändert. Customer- und EnergySystem-Snapshots
werden nicht materialisiert. Es wurde keine Migration, kein Event Bus und
keine Messaging-Infrastruktur ergänzt.
