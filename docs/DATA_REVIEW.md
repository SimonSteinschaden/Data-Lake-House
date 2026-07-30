# Datenprüfung

## Zweck

„Datenprüfung“ ist der UI-Name der intern weiterhin `Curation` genannten
Komponente. Sie enthält ausschließlich Fälle, die eine menschliche
Entscheidung benötigen. Allgemeine Qualitätsstatistik und Reports gehören
nicht in diese Oberfläche.

## Workflow und Entscheidungen

Die Aufgabenliste filtert offene Fälle nach Entität, Feld und Confidence. Die
Detailansicht zeigt Originalwert, normalisierten beziehungsweise
vorgeschlagenen Wert, Quelle, betroffene Entität, Regel und Begründung.
Benutzer können den Vorschlag übernehmen, einen manuellen Wert mit Begründung
setzen, einen Fall begründet ignorieren oder ihn ohne Zustandsänderung später
bearbeiten.

## Audit

`CurationDecision` speichert Benutzer, Zeitpunkt, Entscheidung, Begründung,
Original-, Vorschlags- und neuen Wert. Die Detailansicht zeigt diese
Audit-Historie. Bestätigte Werte gelangen ausschließlich über den Canonical
Snapshot Builder in nachgelagerte Data Products.

## API

Die kompatiblen Endpunkte verbleiben unter `/api/v1/curation/tasks`, inklusive
Detail, Accept, Customize, Reject und Decisions im Detail-Contract. Die
öffentliche UI-Route lautet `/tools/data-review`; alte Curation-Routen leiten
dorthin um.

## Erweiterbarkeit

Weitere Entscheidungstypen (mehrdeutige Einheiten, Energieträger,
Verbrauchs-/Erzeugungstyp, Dubletten und Importkonflikte) werden als weitere
`CurationTask`-Regeln ergänzt, nicht als allgemeine Fehlerlisten.
