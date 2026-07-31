# ENSET-Qualitätsmodell

Qualität und Freigabe sind getrennte Dimensionen. `Bronze`, `Silver` und
`Gold` beschreiben die operative Datenqualität. `Draft`, `Released` und
`Archived` beschreiben ausschließlich die Veröffentlichung eines
Datenprodukts.

Die verbindliche Berechnung liegt in
`HierarchicalQualityAssessment`. React, Controller, Exporte und Berichte
dürfen keine eigene Qualitätslogik besitzen.

Die zugrunde liegenden Erklärungen, Analysen, Issues und Entscheidungen
werden gemäß `HIERARCHICAL_QUALITY_PERSISTENCE.md` versioniert gespeichert;
berechnete Qualitätsstufen werden nicht persistiert.

## Stufen

- Bronze: Pflichtbestandteil fehlt, Inventar ist nicht bestätigt, ein
  untergeordneter Scope ist Bronze oder ein Jahreswert ist nicht belastbar.
- Silver: Daten sind vollständig und technisch analysiert, aber mindestens
  eine fachliche Bestätigung oder Kuration fehlt.
- Gold: alle fachlichen Anforderungen und Inventare sind bestätigt, alle
  relevanten Zählpunkte und Anlagen sind Gold und es gibt keine Blocker.

Die schlechteste erforderliche Teilqualität begrenzt die Gebäudequalität.
Ein Gebäude ohne Zählpunkt wird niemals Gold. Ohne Anlage ist Gold nur mit
bestätigter Nichtanwendbarkeit möglich.

## Fortschritt

`Missing = 0`, `Complete = 1`, `Confirmed = 2`. Der Fortschritt ist die
erreichte Punktzahl geteilt durch die maximal mögliche Punktzahl. Zusätzlich
werden die absoluten Bronze-, Silver- und Gold-Anzahlen ausgegeben. Der
Prozentwert ist kein Qualitätsstatus.

Änderungen an bestätigten Daten, neue untergeordnete Scopes oder neue
Analyseprobleme müssen die aktuelle Projektion neu bewerten. Veröffentlichte
historische Gold-Versionen bleiben unverändert.

Die operative Projektion wird über
`IHierarchicalQualityAssessmentService` batchweise in die Canonical
Snapshots eingebracht. Nachgelagerte Verbraucher übernehmen dieses Ergebnis.
