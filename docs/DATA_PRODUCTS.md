# Data Products

## Architektur

Data Products sind die einzige standardisierte fachliche Ausgabeschicht. Der
zentrale `CanonicalDataProductCatalogService` liest fachliche Werte
ausschließlich über `ICanonicalSnapshotReader`. Kosten- oder CO₂-Werte werden
ohne kanonische Eingangsdaten nicht erfunden. Fehlende Werte bleiben `null`;
unvollständige Jahre werden nicht hochgerechnet.

Der gerichtete Abhängigkeitsgraph ist über
`GET /api/v1/data-product-catalog/dependencies` verfügbar. Zyklen sind durch
einen Architekturtest ausgeschlossen.

## Produktkatalog

Der Katalog enthält Gebäudeenergieprofil, Zählpunkt-Verbrauchsübersicht,
Jahresenergiebilanz, Kosten- und CO₂-Übersicht, Spitzenlastprofil,
Lastdauerlinie, Energieträger- und Nutzungsverteilung, Energiesystem-Inventar,
drei Qualitätsprodukte, Benchmarkprofil, Portfolio-Energieübersicht,
Erneuerbare-Erzeugungsübersicht, ISO-50001-EnPI und LEB-Exportdatensatz.

## Metadaten und Versionierung

Jeder Eintrag besitzt Code, englischen und deutschen Namen, Beschreibung,
Kategorie, Owner, Inputs, verwendete Produkte, Output-Schema, Quelle,
Snapshot-Version, Quality Level, Suitability, Aktualisierung, Exporte,
Endpoint, Zeitraum, Aggregation, Missing-Data-Verhalten und Lineage.
Versionen folgen `Major.Minor.Patch`; der initiale Contract ist `1.0.0`.

## API und Export

- `GET /api/v1/data-product-catalog`
- `GET /api/v1/data-product-catalog/{code}`
- `GET /api/v1/data-product-catalog/{code}/schema`
- `GET /api/v1/data-product-catalog/{code}/preview`
- `GET /api/v1/data-product-catalog/{code}/export?format=json|csv|xlsx`

Vorschau und Export werden aus derselben Data-Product-Projektion erzeugt.
Der bestehende LEB-Exportpfad und seine Formate bleiben unverändert.
