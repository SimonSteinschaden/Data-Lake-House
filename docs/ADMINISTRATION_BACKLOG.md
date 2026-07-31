# Administration Backlog

Die Administration wird erst mit produktiver Authentifizierungs-, Rollen- und
Mandantenverwaltung umgesetzt.

| Modul | Zweck | Abhängigkeiten | Berechtigung | Priorität | Phase |
|---|---|---|---|---|---|
| Benutzer | Konten und Status | Identity Provider | UserAdmin | P0 | Post-MVP |
| Rollen | Rollenmodell | Authentifizierung | RoleAdmin | P0 | Post-MVP |
| Mandanten | Mandantentrennung | Tenant-Konzept | TenantAdmin | P0 | Post-MVP |
| Berechtigungen | Policies und Scopes | Rollen, Mandanten | SecurityAdmin | P0 | Post-MVP |
| Einheiten | kontrollierte Einheiten | Domain Governance | DataAdmin | P1 | Post-MVP |
| Energieträger | Referenzwerte | Domain Governance | DataAdmin | P1 | Post-MVP |
| Anlagentypen | Anlagenkatalog | EnergySystem-Modell | DataAdmin | P1 | Post-MVP |
| Importprofile | wiederverwendbare Mappings | Import Pipeline | ImportAdmin | P1 | Post-MVP |
| Systemparameter | Laufzeitkonfiguration | Configuration Store | SystemAdmin | P1 | Post-MVP |
| Audit | Recherche/Retention | Audit Store | Auditor | P1 | Post-MVP |
| Jobs | Überwachung und Neustart | Worker | JobAdmin | P1 | Post-MVP |
| Integrationen | externe Verbindungen | Secret Store | IntegrationAdmin | P2 | Post-MVP |
