# Data Product Engine 1.0 RC

## Begriffe und Datenfluss

Eine Definition legt Code, Kategorie, Ergebnistyp und erlaubte Scopes fest. Ein DataProduct ist eine konkrete Produktinstanz. ScopeAssignment verbindet sie mit Meter, Building oder einem weiteren Domainobjekt. CustomerAssignment bildet die derzeitige Autorisierung ab.

```text
PostgreSQL/Data Lake
  → EfDataProductReader
  → Application Reader Port
  → IDataProductGenerator
  → DataProductGenerationContext
  → DataProductGenerationRun
  → DataProductVersion
  → DataProductValue
  → REST DTO
  → React Dashboard
```

Ein Upload ist ausschließlich ein möglicher Ingestion-Weg. Generatoren kennen keine Dateien, Importformate oder EF-Core-Abfragen.

## Generierung

`DataProductGenerationService` lädt das Produkt, prüft CustomerAssignment, fordert genau einen Scope, prüft Availability, löst den Generator über `Definition.Code` auf und legt erst unmittelbar vor `GenerateAsync` einen Running-Run an. Leere Values werden abgelehnt. Nach erfolgreicher Berechnung wird die nächste Versionsnummer über `GetNextVersionNumberAsync` bestimmt. Fehler und Cancellation aktualisieren einen bereits gestarteten Run auf Failed beziehungsweise Cancelled.

Availability verwendet denselben fachlichen Scope und die Reader wie der Generator. Es wird nicht auf Upload-Metadaten geprüft.

## MVP-Produkte

- `METER_CONSUMPTION_SUMMARY`: Gesamtverbrauch, Reading Count, Meter Count, Datenqualität.
- `BUILDING_ENERGY_PROFILE`: Gesamtverbrauch, Grundlast als fünftes Perzentil, Spitzenlast, Meter Count, Datenqualität.

Unterstützt werden Intervallenergie, kumulative Zählerstände und eindeutig typisierte Leistungswerte. Negative kumulative Differenzen werden als Warnung behandelt. Energie und Leistung werden zentral nach kWh beziehungsweise kW konvertiert.

## Einschränkungen

- Keine Haupt-/Unterzählerhierarchie zur automatischen Vermeidung von Doppelzählung.
- Authorization basiert auf CustomerAssignment und übergebener MVP-User-ID, nicht auf Claims/Rollen.
- Versionsnummern sind per Unique Index geschützt, aber konkurrierende Generierungen besitzen noch keinen expliziten Retry-/Lock-Mechanismus.
- Development-Seeds werden nur im Development-Host ausgeführt.
