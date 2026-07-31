# ADR: LEB als erstes External Data Product

Status: Accepted

## Kontext und Entscheidung

ENSET benötigt einen stabilen Behördenexport mit nachvollziehbarem,
versioniertem Schema. `NoeLebExportContractV1` wird deshalb als erstes
offizielles External Data Product eingeführt.

LEB ist kein frei konfigurierbares Analyseprodukt und kein Bestandteil eines
zukünftigen Data Marketplace. Der Vertrag ist auf den Datenaustausch mit der
Landesenergiebuchhaltung begrenzt. Internal Data Products bleiben interne
fachliche ReadModels für Dashboard und Detailansichten; der LEB-Vertrag
transformiert freigegebene und bestehende Daten in ein externes Behördenschema.

## Architektur

```text
Gold Profiles
      |
      v
Internal Data Products + bestehende ReadModels/MeterReadings
      |
      v
NoeLebExportContractV1
      |
      v
LebExportValidator
      |
      +---------> CSV-ZIP
      |
      +---------> XLSX
      |
      `---------> Siemens Navigator (später)
```

## Konsequenzen

- Jeder Dateiexport wird vor der Serialisierung vollständig validiert.
- CSV und Excel serialisieren ausschließlich den versionierten Vertrag.
- Mappings sind zentral und unbekannte Werte werden nicht geraten.
- Es entstehen keine Domain Entity, Migration, Benchmark-, Kosten-, CO₂- oder
  Zeitreihenberechnung.
- Ein späterer Navigator-Connector kann denselben validierten Vertrag
  konsumieren, ohne Internal Products oder Domainmodelle nach außen zu
  veröffentlichen.
