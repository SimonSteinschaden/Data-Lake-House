# Technische TODOs

Die priorisierte Roadmap steht in `../11_Roadmap.md`. Diese Liste hält konkrete Implementierungslücken fest.

## Importworkflow

- JSON-Reportrepository durch produktive Datenbankpersistenz mit Concurrency Control ergänzen.
- API-UserId durch authentifizierten Benutzerkontext ersetzen.
- Audit Trail um unveränderliche Persistenz und fachliche Korrelationsdaten erweitern.
- Write-Vorgänge transaktional und idempotent ausführen.
- Customer-Pipeline auf Buildings, Meter und MeterReadings erweitern.
- Hart codierten XLSM-Pfad in `Program.cs` durch Argumente oder Konfiguration ersetzen.

## Qualität und Tests

- vorhandene Tests um `ImportDecisionEngine`, Excel-End-to-End, Database- und Raw-Zone-Integration erweitern.
- Architekturtests für Layer- und ClosedXML-Grenzen.
- Leere Mapper-, Validator-, AutoFix- und Normalizer-Platzhalter implementieren oder entfernen.
- Encoding und Formatierung der verbleibenden Quellcodedateien vereinheitlichen.
- bereits versionierte `bin`-/`obj`-Artefakte aus Git entfernen; `.gitignore` ist vorhanden.

## API und UI

- OpenAPI, versionierte Fehlerverträge sowie Upload-/Content-Sicherheitsprüfung ergänzen.
- React Import Wizard implementieren.
- Authentifizierung, Autorisierung und Freigaberollen definieren.

## Storage und Betrieb

- fachliches und transaktionales Mapping im vorbereiteten `DatabaseImportWriter` implementieren.
- dateibasierten RawZoneWriter für produktiven Storage, Retention und Zugriffsschutz härten.
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
