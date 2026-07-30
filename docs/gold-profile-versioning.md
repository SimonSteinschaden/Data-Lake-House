# Gold-Profil-Versionierung und Data Product Readiness

> Phase-B-Hinweis: `GoldProfileVersion` bleibt die persistierte Versions- und
> Freigabestruktur. Ihr fachlicher Inhalt ist Teil der allgemeinen
> Canonical-Snapshot-Schicht. `Released`, `IsCurrent` und das Quality Level
> `Gold` sind unabhängige Zustände. Internal Data Products konsumieren
> ausschließlich `ICanonicalSnapshotReader` und lesen Gold-Versionen oder
> `CuratedFieldValues` nicht direkt.

Gold-Profile können als unveränderliche Snapshots versioniert werden. Der
SHA-256-Hash enthält ausschließlich den fachlichen Snapshot; Datenbank-ID,
Ersteller, Zeitstempel und `xmin` gehören nicht zum Hash. Ein identischer Hash
liefert die aktuelle Version zurück. Bei fachlicher Änderung wird die bisherige
Version zeitlich geschlossen und eine neue Draft-Version erzeugt.

Release-Status:

- `Draft`: erzeugt, noch nicht produktiv verwendbar
- `Released`: nach bestandener Gold-Readiness verwendbar
- `Superseded`: durch eine neuere Freigabe ersetzt
- `Revoked`: fachlich zurückgezogen

Erstellung, Freigabe, Ersetzung und Rücknahme werden mit Benutzer, Zeitpunkt,
Statusübergang, Grund und SnapshotHash protokolliert. Veröffentlichte Versionen
werden nicht gelöscht.

Die Data-Product-Readiness verwendet gewichtete, deterministische Anforderungen.
Der Prozentwert ist die Summe erfüllter Gewichte geteilt durch alle relevanten
Gewichte. Blockierende Anforderungen verhindern unabhängig vom Prozentwert den
Status `Ready`. Jede fehlende Anforderung enthält eine Handlungsempfehlung.

Unterstützt werden Building/Energy Benchmark, normalisiertes Last- und
Erzeugungsprofil, EEG Matching und Peer-to-Peer Analysis. Readiness bedeutet
nicht, dass Berechnung oder Persistenz dieser Data Products implementiert ist.
Fehlende Netz- und Tarifmodelle werden ausdrücklich als Blocker ausgegeben.

APIs:

- `/api/v1/gold-profiles/{entityType}/{entityId}/...`
- `/api/v1/data-product-readiness/{dataProductType}/{scopeType}/{scopeId}`
- `/api/v1/data-product-readiness/{scopeType}/{scopeId}`
