# Reporting-Architektur

## Trennung

Die Objektanalyse dient der interaktiven Exploration mit Suche, Filtern,
Zeitraumwechsel und Ursachenanalyse. Ein Report besitzt eine feste
Berichtsperiode, Empfänger, Version, Erstellungszeit und Freigabestatus.

Beide verwenden dasselbe `ObjectAnalyticsProduct`. Bei der Reporterzeugung
wird dieses Produkt vollständig in der `ReportInstance` eingefroren.
Nachträgliche Snapshot-Änderungen verändern einen bestehenden Report oder
dessen Exporte daher nicht.

## Komponenten

- `ReportDefinition`: Katalog und unterstützte Formate.
- `ReportInstance`: versionierter, reproduzierbarer Analytics-Snapshot.
- `IReportService`: Erzeugung, Liste, Detail und Export.
- `FileReportService`: dateibasierte, atomar serialisierte Instanzen sowie
  PDF-, Excel- und JSON-Rendering.
- `ReportsController`: reine HTTP-Orchestrierung ohne Berichtslogik.

Instanzen liegen unter `App_Data/report-instances`. Neue Versionen werden je
Reporttyp, Objekt und identischer Periode fortlaufend nummeriert.

## Reporttypen

Der Katalog enthält Objekt-Energiebericht, Jahresenergiebericht,
Verbrauchsbericht, Kostenbericht, CO₂-Bericht, Lastprofilbericht,
Anlagenbericht, Datenqualitätsbericht, Portfoliovergleich,
ISO-50001-Auswertung und Landesenergiebuchhaltungsbericht.

Alle Typen verwenden zunächst dieselbe konsolidierte Reportstruktur. Kosten-
und CO₂-Berichte kennzeichnen Werte als nicht verfügbar, bis freigegebene
Tarif- beziehungsweise Emissionsfaktor-Produkte existieren.

## Struktur und Exporte

Reports enthalten Titel, Objekt, Periode, Version, Erstellungsdatum,
Empfänger, Quality, Suitability, Kennzahlen, Zeitreihen, Warnungen,
Vollständigkeit, Benchmark, Anlagen und Quellen.

Unterstützt werden:

- PDF als reproduzierbares, minimales PDF-Dokument;
- Excel mit Summary- und Monatsverbrauch-Worksheet;
- JSON als vollständiger maschinenlesbarer Reportvertrag.

Die Renderer lesen ausschließlich die eingefrorene `ReportInstance`.

## Versionierung und Freigabe

Der aktuelle MVP erzeugt neue Instanzen mit `Draft`. Der Contract kennt
zusätzlich `Released` und `Archived`; eine rollenbasierte Freigabeoperation
ist eine Erweiterung. Exporte enthalten den gespeicherten Status und werden
nicht zur Laufzeit fachlich neu berechnet.

## Erweiterbarkeit

Spätere Renderer können denselben Vertrag für gestaltete PDF-Templates,
Signaturen oder Archivsysteme verwenden. Tarif-, Emissions- und ISO-Regeln
sollen als eigene freigegebene Data Products ergänzt werden. Eine
PostgreSQL-Persistenz kann den dateibasierten Adapter ersetzen, ohne
Analytics- oder Controllerlogik zu verändern.
