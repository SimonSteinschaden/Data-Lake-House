# Benutzer-, Mandanten- und Autorisierungsarchitektur

## Rollenmodell

`EnsetEmployee` und `EnsetAdmin` sind globale Systemrollen und heben den Customer-Filter auf. Die Rollen
`CustomerAdmin`, `CustomerUser` und `CustomerViewer` gelten ausschließlich innerhalb einer
aktiven, zeitlich gültigen `UserCustomerAssignment`. Ein Benutzer kann dadurch je Customer
unterschiedliche Rollen besitzen und zusätzlich EnsetEmployee sein.

| Rolle | Lesen | Fachlich schreiben | Benutzer verwalten |
|---|---:|---:|---:|
| EnsetEmployee | systemweit | systemweit | systemweit |
| EnsetAdmin | systemweit | systemweit | systemweit |
| CustomerAdmin | eigener Customer | ja | eigener Customer |
| CustomerUser | eigener Customer | ja | nein |
| CustomerViewer | eigener Customer | nein | nein |

Eine Assignment ist gültig, wenn `IsActive`, `ValidFrom <= UtcNow` und entweder kein
`ValidTo` gesetzt oder `ValidTo > UtcNow` ist. Ein partieller eindeutiger Datenbankindex
verhindert mehrere gleichzeitig als aktiv markierte Assignments desselben Users zum selben
Customer.

## Benutzer und externe Identität

`ApplicationUser.Id` ist der interne Schlüssel. `ExternalIdentity` ist der eindeutige,
providerunabhängige Identitätswert. Er wird primär aus dem JWT-Subject übernommen. Passwörter
und Login werden
nicht von der ENSET-Domäne verwaltet.

## CurrentUserContext

`ICurrentUserContext` liegt im Application Layer und kennt weder `HttpContext` noch Claims.
JwtBearer validiert Signatur, Issuer, Audience und Lebensdauer. Der `ICurrentUserResolver` lädt
Benutzer und aktuell gültige Rollen aus PostgreSQL und initialisiert den scoped Context.
Im Development stellt ein lokaler Token-Endpunkt ein JWT für den geseedeten Benutzer aus.

Für einen externen OIDC-Provider werden später Issuer, Audience und Signing-Metadaten angepasst:

```text
JWT subject -> ExternalIdentity -> ApplicationUser -> CurrentUserContext
```

Application Services und `IDataAccessScope` bleiben unverändert.

## Mandanten- und Objektscope

`IDataAccessScope` komponiert `IQueryable`-Filter; erlaubte Customer-IDs werden nicht vorab
materialisiert. Damit entstehen korrelierte SQL-Unterabfragen statt N+1-Abfragen.

```text
UserCustomerAssignment -> Customer -> CustomerBuildingAssignment -> Building
Building -> Meter -> MeterReading
Customer -> Project -> Document
```

Meter ohne Building sind für Customer-Rollen vorerst nicht sichtbar. Die zusätzliche
Customer-Herleitung über eigenständige EnergySystems bleibt ein offener Erweiterungspunkt.
Documents sind im aktuellen Modell eindeutig über `Document.Project.CustomerId` zuordenbar.

Die Policies `Authenticated`, `EnsetEmployee`, `CustomerReader`, `CustomerWriter` und
`CustomerAdmin` sind Grobfilter. Objektzugriffe müssen zusätzlich über `IDataAccessScope`
erfolgen. Nicht sichtbare Objekte liefern 404; sichtbare Objekte ohne Schreibrecht 403;
nicht authentifizierte Zugriffe 401.

## Importvorbereitung

`ImportReport` und seine relationale Persistenz führen optional `CreatedByUserId` und
`CustomerId`. Die Analyse übernimmt den internen Benutzer aus `ICurrentUserContext`.
Eine vollständige Customer-Einschränkung des Importflows ist bewusst noch nicht aktiviert.

## Development-Seed

Der idempotente Development-Seed erzeugt einen Test-Customer und folgende externe Identitäten:

- `development-user` – EnsetEmployee
- `development-customer-admin` – CustomerAdmin
- `development-customer-user` – CustomerUser
- `development-customer-viewer` – CustomerViewer

Die drei Customer-Benutzer werden dem Development-Customer zugeordnet.
