# ADR: Hierarchisches Qualitätsmodell

## Entscheidung

Die Qualitätsbewertung wird als zentrale, deterministische Projektion über
Canonical Snapshots modelliert. Gebäude aggregieren Building Core,
Zählpunkte, Anlagen, Inventarerklärungen und Jahresenergiestatus. Die
schlechteste erforderliche Teilqualität begrenzt das Ergebnis.

Qualität ist von der Freigabe eines Datenprodukts getrennt. Technische
Profilanalyse liefert Befunde, vergibt aber kein Gold. Gold benötigt
serverseitig autorisierte fachliche Bestätigung. Kundenrollen erhalten diese
Berechtigung nicht.

## Folgen

Alle Verbraucher verwenden dasselbe Assessment. Neue Scopes und geänderte
bestätigte Werte führen zu einer Neubewertung der operativen Projektion.
Historische freigegebene Versionen werden nicht rückwirkend verändert.

Die Phase-1-Persistenz speichert ausschließlich reproduzierbare Eingaben
und Auditdaten. Ein partieller Unique-Index schützt die jeweilige
`IsCurrent`-Semantik; historische Datensätze werden durch restriktive
Fremdschlüssel vor Kaskadenlöschung geschützt.

Die produktive Integration erfolgt ausschließlich additiv über Canonical
Snapshots. Controller, React-Komponenten, Reports und Exporte besitzen keine
eigene Bronze-/Silver-/Gold-Regel.
