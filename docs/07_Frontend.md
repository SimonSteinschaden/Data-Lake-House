# Frontend

## Aktueller Stand

Im Repository existiert keine Benutzeroberfläche. Es gibt weder ein React-/Next.js-Projekt noch WinUI, Blazor oder einen anderen UI-Client. `Enset.Worker` ist ein Konsolen-Testpfad und keine UI.

## Für den Import Wizard bereits verfügbare Modelle

- `ImportReport` mit Import-ID, Zeitpunkt, Customers, Issues, Decision und Statistiken;
- stabile `ImportIssue.IssueId` zur Zuordnung von Entscheidungen;
- ResolutionActions und benutzerdefinierte Resolution-Werte;
- vorbereitende Request-/Response-DTOs.

## Noch offen gemäß Baseline v1.0

- REST API als Voraussetzung;
- React Import Wizard für Upload, Analyse, Issue-Prüfung und Bestätigung;
- Anzeige von Severity, Entscheidungsbedarf und ResolutionActions;
- Fehler-, Lade- und Wiederaufnahmezustände;
- Authentifizierung und rollenbasierte Freigabe;
- UI-, Accessibility- und End-to-End-Tests.

Die konkrete UX und technische Frontend-Ausgestaltung sind noch nicht implementiert.
