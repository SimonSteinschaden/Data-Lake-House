# Analytics Data Products

## Leitprinzip

Management Dashboard, Objektanalyse, Reports und Business Modules konsumieren
ausschließlich stabile Analytics-Data-Products. Das Frontend greift weder
direkt auf Tabellen noch auf fachliche Entity-Listen zu und berechnet keine
fachlichen Kennzahlen.

```text
Quellsysteme → Import → kanonisches Data Lake House
             → serverseitige Analytics → Data Products → Konsumenten
```

Der Analytics-Vertical-Slice besteht aus:

- Application: unveränderliche Produktmodelle und
  `IAnalyticsDataProductService`;
- Infrastructure: EF-basierte, serverseitige Selektion, Normalisierung und
  Aggregation;
- API: genau ein versionierter GET-Endpunkt je Produkt;
- Web: typisierte Leseservices und reine Darstellung mit Empty States.

Die bestehende versionierte Data-Product-Generation bleibt für materialisierte
und publizierbare Produkte bestehen. Die Management-Produkte sind zunächst
read-only Query-Produkte mit `CalculatedAt`; sie können später über denselben
fachlichen Vertrag materialisiert und versioniert werden.

## Implementierte Management-Produkte

| Data Product | Berechnung |
|---|---|
| `PortfolioSummary` | Anzahl Customers, Buildings, Meters, Documents und EnergySystems |
| `RegionalBuildingDistribution` | Gruppierung aktueller BuildingVersion-Adressen nach Bundesland, Bezirk, Gemeinde und PLZ |
| `ElectricityPortfolioLoadProfile` | ausschließlich elektrische Leistungswerte; W/MW werden nach kW normalisiert; Aggregation nach Zeitraster |
| `MonthlyElectricityConsumption` | ausschließlich Intervall-Energiewerte; Wh/MWh werden nach kWh normalisiert; Monatswerte und Vorjahr |
| `MeteringCoverageSummary` | Gebäude/Zähler mit beziehungsweise ohne Messwerte, Aktivstatus und letzter Messwert |
| `DataQualitySummary` | fehlende Meter-Zuordnung, 30-Tage-Aktualität, unvollständige Building-Stammdaten, unbekannte Einheiten und ungültige Messwerte |
| `EnergyPortfolioStructure` | Gebäude und Zähler nach Energieträger sowie Anlagen nach Typ |
| `ManagementWarnings` | serverseitig abgeleitete Hinweise für Zuordnung, Aktualität und Einheit |
| `EnergyConsumptionByLocation` | Top Buildings aus normalisierten Intervall-Energiewerten |
| `EnergyConsumptionByUsageType` | leer, bis eine kanonische UsageType-Zuordnung existiert |
| `TopEnergySystemsByConsumption` | Top EnergySystems mit direkt zugeordneten Energiezählern |
| `EnergyConsumptionByCarrier` | Energieträger nur nach gemeinsamer Normalisierung auf kWh |

## Dimensionsregeln

- Ein Lastprofil ist eine Leistungszeitreihe. Es verwendet nur Meter mit
  `Quantity=Power` und kompatiblen Einheiten W, kW oder MW.
- Monatsverbrauch ist Energie. Er verwendet nur
  `ReadingType=IntervalValue`, `Quantity=Energy` und Wh, kWh oder MWh.
- Kumulative Zählerstände werden nicht summiert.
- Jahres- oder Monatssummen werden nicht künstlich auf Zeitstempel verteilt.
- Nicht konvertierbare Einheiten werden nicht vermischt.
- Leere Resultate bleiben leer und werden im Frontend als Empty State gezeigt.

## Filter

Die Zeitreihen- und Verbrauchsprodukte akzeptieren Jahr, Kunde, Gebäude,
Zähler, Region, Postleitzahl und Aggregation. Der API-Vertrag ist damit stabil.
Die Dashboard-Auswahl für Objektfilter bleibt deaktiviert, bis eigene
Filteroptions-Produkte beziehungsweise autorisierte Suchendpunkte verfügbar
sind.

## Offene fachliche Annahmen

- `UsageType` ist noch nicht kanonisch einem Meter oder EnergySystem
  zugeordnet. Eine Ableitung aus Namen findet daher nicht statt.
- Gas, Biomasse und andere Träger benötigen fachlich gepflegte
  Umrechnungs-/Heizwerte, falls Quelldaten nicht bereits Energieeinheiten
  liefern.
- Zeitreihenlücken benötigen Soll-Raster, Zeitzone und erwartete
  Betriebszeiträume. Bis dahin ist der Wert `null`, nicht künstlich `0`.
- Dublettenerkennung benötigt einen freigegebenen fachlichen Schlüssel. Bis
  dahin ist der Wert `null`.
- Der Data-Quality-Score ist im MVP ein transparenter Bestandsindikator aus
  Zuordnung, Aktualität, Stammdaten und Einheiten. Gewichtungen müssen fachlich
  versioniert werden.
- Kosten, CO₂, Benchmarks, Einsparungen, Peaks, Flexibilität und Optimierung
  benötigen Tarife, Emissionsfaktoren beziehungsweise freigegebene
  Berechnungsmodelle. P1/P2-Produkte werden erst mit diesen Grundlagen
  fachlich implementiert und liefern vorher keine Demo-Werte.
