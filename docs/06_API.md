# REST API

## Aktueller Stand

`Enset.Api` ist eine ASP.NET-Core-API auf .NET 10. Controller greifen ausschließlich auf Application-Use-Cases und Repository-Ports zu; sie injizieren oder rufen keinen `IImportWriter` direkt auf.

## Endpunkte

### Mandantengesicherte Stammdaten-Reads

Alle Read-Endpunkte verwenden die Policy `CustomerReader`. Die Identität wird aus einem
validierten JWT aufgelöst; Controller lesen keine Authentifizierungsheader und greifen
nicht auf den DbContext zu. Nicht sichtbare Details werden wie nicht vorhandene Ressourcen mit
404 behandelt.

- `GET /api/v1/customers` und `GET /api/v1/customers/{customerId}`
- `GET /api/v1/buildings` und `GET /api/v1/buildings/{buildingId}`
- `GET /api/v1/meters` und `GET /api/v1/meters/{meterId}`

Listen unterstützen `search`, `page`, `pageSize`, `sortBy` und `sortDirection`; fachlich
passende Filter sind `isActive`, `customerId` und `buildingId`. `pageSize` ist auf 200 begrenzt.
Counts sowie Messwert-Minimum/-Maximum werden innerhalb der SQL-Projektion berechnet.

### GET `/api/v1/meters/{meterId}/readings`

Query-Parameter:

- `from` inklusive und `to` exklusiv, jeweils UTC;
- `aggregation`: `raw`, `fifteenMinutes`, `hour`, `day` oder `month`;
- `page`, `pageSize` und `sortDirection`.

Rohwerte sind immer paginiert, standardmäßig mit 100 und maximal 200 Elementen. Aggregationen
gruppieren bereits in PostgreSQL nach UTC-Zeitkomponenten. Jeder Bucket enthält Minimum,
Maximum, Durchschnitt, FirstValue, LastValue, Delta und Count. `Sum` wird ausschließlich für
`IntervalValue` berechnet. Kumulative und momentane Werte werden nicht summiert. Bei gemischten
ReadingTypes bleiben Buckets typgetrennt. Ungültige Zeiträume liefern ProblemDetails mit 400;
ein fremder oder unbekannter Meter liefert 404.

### POST `/api/v1/imports/analyze`

- erwartet `multipart/form-data` mit Feld `file` (`.xlsx` oder `.xlsm`);
- verwendet den authentifizierten Benutzer für den Auditkontext;
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
# Data Products API

- `GET /api/v1/data-products`
- `GET /api/v1/data-products/{id}`
- `GET /api/v1/data-products/{id}/generation-availability`
- `POST /api/v1/data-products/{id}/generate`
- `GET /api/v1/data-products/{id}/versions/latest`
- `GET /api/v1/data-products/{id}/versions`

Die API serialisiert ausschließlich Contracts/DTOs. Fachliche Fehler werden als `ProblemDetails` zurückgegeben. Die Benutzeridentität stammt aus dem validierten Bearer Token.
# LEB-Analyseparameter

`POST /api/v1/imports/analyze` bleibt `multipart/form-data` und akzeptiert
zusätzlich zu `ImportFile` die Felder `SourceType` und `Medium`.
`SourceType` ist `EnsetWorkbook` (Standardwert, rückwärtskompatibel) oder
`Landesenergiebuchhaltung`. Für `Landesenergiebuchhaltung` ist `Medium` mit
`Electricity` oder `Heat` verpflichtend.

`POST /api/v1/imports/{importId}/resolutions` wendet alle Entscheidungen als
Batch an, ruft anschließend zentral
`ImportReport.RecalculateCommitReadiness()` auf und persistiert den
aktualisierten Report. Die Response enthält zusätzlich:

```json
{
  "status": "AwaitingResolution",
  "unresolvedIssueCount": 12,
  "readinessMessage": "12 Issues noch ungelöst."
}
```

`unresolvedIssueCount` zählt ausschließlich Issues mit
`ImportIssue.IsCommitBlocking`. Bei `ReadyToCommit` ist der Wert `0` und
`readinessMessage` ist `null`.

### Gruppenentscheidung

`POST /api/v1/imports/{importId}/resolution-rules` wendet eine einzelne
strukturierte Regel auf ein Issue oder alle passenden Issues des aktuellen
Imports an. Die Antwort enthält `matchedIssueCount`, `resolvedIssueCount`,
`remainingBlockingIssueCount`, `status` und den aktualisierten Report.
Wiederholungen mit derselben `ruleId` sind idempotent.
