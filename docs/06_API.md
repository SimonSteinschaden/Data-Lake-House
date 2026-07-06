# REST API

## Aktueller Stand

Es existiert noch kein ASP.NET-Core-API-Projekt, kein Controller, kein HTTP-Endpunkt und keine OpenAPI-Konfiguration.

In `Enset.Application/Imports/DTOs/Api` sind ausschließlich vorbereitende Verträge vorhanden:

- `ImportIssueResponseDto`
- `ImportReportResponseDto`
- `ApplyImportResolutionRequest`

Diese DTOs werden noch nicht durch Endpunkte oder Mapper verwendet. `ImportReport` wird nicht persistiert; deshalb kann eine API aktuell keinen Report anhand der `ImportId` wieder laden.

## Verbleibende Arbeiten gemäß Baseline v1.0

- API-Projekt und Composition Root einrichten;
- Analyse-Endpunkt für Imports bereitstellen;
- Reportpersistenz und Abfrage über `ImportId` implementieren;
- Resolution-Endpunkt auf `IApplyResolutionService` aufbauen;
- DTO-Mapping und Eingabevalidierung ergänzen;
- Authentifizierung, Autorisierung und Audit-Kontext definieren;
- OpenAPI-/Swagger-Dokumentation und Fehlerverträge bereitstellen;
- Integrations- und Sicherheitstests ergänzen.

Dieses Dokument legt keine neuen Endpunkte oder Zielarchitektur fest.
