# ADR: Canonical Snapshot als Single Source of Truth

Status: Accepted  
Datum: 2026-07-30

## Kontext

Internal Data Products leiteten fachliche Werte bisher parallel aus relationalen
Entities, `CuratedFieldValues`, Gold-Profilen und eigenen Berechnungen ab. Dadurch
konnten Listen, Products und Readiness unterschiedliche Aussagen liefern.

## Entscheidung

Canonical Snapshots sind die einzige fachliche Quelle für Internal Data
Products.

- Bestätigte kuratierte Werte besitzen Vorrang vor gültigen Originalwerten.
- Fehlende Werte bleiben null.
- Bronze, Silver und Gold sind Qualitätsstufen und keine globalen Filter.
- Suitability beschreibt ausschließlich die Eignung für einen konkreten
  Anwendungsfall.
- Release-Status, Quality Level und Suitability bleiben getrennte Konzepte.
- Jahreswerte werden zentral und ohne Hochrechnung bestimmt.
- ImportQuality darf technische Import- und Auditdaten weiterhin direkt lesen.

Die vorhandene Gold-Profil-Versionierung wird als Versionsmetadatenbasis
weiterverwendet. Es wird keine parallele Snapshot-Persistenz eingeführt.

## Konsequenzen

- Product Services hängen von `ICanonicalSnapshotReader` ab.
- `CuratedFieldValues` werden ausschließlich im Snapshot-Builder gelesen.
- Unvollständige Datensätze bleiben in Products enthalten.
- REST-Modelle weisen Quality Level und anwendungsfallspezifische Suitability
  getrennt aus.
- Der LEB-Export bleibt in diesem Arbeitspaket unverändert.

Für Customer und EnergySystem existieren noch keine dauerhaft materialisierten
allgemeinen Versionen. Bis zu deren kontrollierter Erweiterung werden
deterministische technische Versionsmetadaten verwendet.
