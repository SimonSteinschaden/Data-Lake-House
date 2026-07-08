# REST API

## Aktueller Stand

`Enset.Api` ist eine ASP.NET-Core-API auf .NET 10. Controller greifen ausschließlich auf Application-Use-Cases und Repository-Ports zu; sie injizieren oder rufen keinen `IImportWriter` direkt auf.

## Endpunkte

### POST `/api/v1/imports/analyze`

- erwartet `multipart/form-data` mit Feld `file` (`.xlsx` oder `.xlsm`);
- erwartet den Header `X-User-Id` für den Auditkontext;
- staged und hasht die Originaldatei;
- führt ausschließlich `ImportCoordinator` über `IImportAnalysisService` aus;
- persistiert und liefert den `ImportReport`.

### GET `/api/v1/imports/{importId}`

- lädt einen persistierten Report über `IImportReportRepository`;
- liefert 404 für unbekannte ImportIds.

### POST `/api/v1/imports/{importId}/resolutions`

- erwartet UserId und eine Liste von Issue-Resolutionen;
- verwendet `IApplyResolutionService`;
- erlaubt wiederholte Änderungen vor dem Commit;
- speichert Audit Trail und Reportstatus;
- führt keinen Writer aus.

### POST `/api/v1/imports/{importId}/commit`

- erwartet UserId, TargetMode, TargetWriter, optionale TargetLocation und Raw-Zone-Option;
- verwendet ausschließlich `IImportCommitService`;
- erzeugt intern den `ImportWriteContext` und wertet `IImportWriteGate` aus;
- ruft erst nach erfolgreichem Gate den ausgewählten `IImportWriter` auf;
- liefert Gate-Fehler als Conflict-Response.

## Persistenz und Datenschutz

`JsonImportReportRepository` ist eine austauschbare dateibasierte Implementierung des Application-Ports. API-Responses enthalten Source-Metadaten und Hash, aber keine internen Staging- oder Raw-Zone-Pfade.

## Noch offen

- OpenAPI/Swagger und versionierte API-Fehlerverträge;
- Authentifizierung/Autorisierung statt übermittelter UserId;
- Uploadgrößen-, Malware- und Content-Prüfung;
- datenbankgestütztes Repository mit Concurrency Control;
- API-Integrations-, Sicherheits- und End-to-End-Tests;
- React-Client.
