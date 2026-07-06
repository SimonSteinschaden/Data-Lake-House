# Technische TODOs

Die priorisierte Roadmap steht in `../11_Roadmap.md`. Diese Liste hält konkrete Implementierungslücken fest.

## Importworkflow

- `ImportReport` persistent speichern und über `ImportId` abrufbar machen.
- API-/Worker-Aufrufer für `ApplyResolutionService -> ImportWriteGate -> IImportWriter` implementieren.
- Entscheidungen um Benutzeridentität, Entscheidungszeitpunkt und Audit Trail ergänzen.
- Write-Vorgänge transaktional und idempotent ausführen.
- Customer-Pipeline auf Buildings, Meter und MeterReadings erweitern.
- Hart codierten XLSM-Pfad in `Program.cs` durch Argumente oder Konfiguration ersetzen.

## Qualität und Tests

- Unit Tests für `ImportCoordinator`, `ImportDecisionEngine`, `ApplyResolutionService` und `ImportWriteGate`.
- IntegrationsTests für Excel-Reader/-Writer und später Database-/Raw-Zone-Writer.
- Architekturtests für Layer- und ClosedXML-Grenzen.
- Leere Mapper-, Validator-, AutoFix- und Normalizer-Platzhalter implementieren oder entfernen.
- Encoding und Formatierung der verbleibenden Quellcodedateien vereinheitlichen.
- `.gitignore` ergänzen und versionierte `bin`-/`obj`-Artefakte aus Git entfernen.

## API und UI

- REST API, DTO-Mapping, OpenAPI und Fehlerverträge implementieren.
- React Import Wizard implementieren.
- Authentifizierung, Autorisierung und Freigaberollen definieren.

## Storage und Betrieb

- `DatabaseImportWriter` und `RawZoneWriter` implementieren.
- Import History und Background Jobs bereitstellen.
- externe Konfiguration, strukturiertes Logging, Monitoring, Docker und CI/CD ergänzen.

## Datenschutz

- Zählpunktnummern für Analysen niemals im Klartext speichern.
- Deterministische Pseudonymisierung definieren, beispielsweise:

```text
MeterNumber: AT001234...
  -> HMACSHA256(secret, normalizedMeterNumber)
  -> MeterPrivacyKey
```

- Secret-Verwaltung, Schlüsselrotation und Berechtigungskonzept dokumentieren.
