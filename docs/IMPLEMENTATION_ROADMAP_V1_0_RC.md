# Implementation Roadmap 1.0 RC

Dieses Dokument enthält ausschließlich offene Punkte.

## P0

| Beschreibung | Priorität | Status | Abhängigkeiten |
|---|---:|---|---|
| Relationalen `DatabaseImportWriter` implementieren und transaktional testen | P0 | Offen | finalisiertes Import-Mapping, PostgreSQL-Testumgebung |
| Worker vom fest codierten lokalen Pfad auf Konfiguration/Argumente umstellen | P0 | Offen | Betriebs- und Deploymentkonzept |
| Echte Authentifizierung und Claims-basierte Benutzeridentität einführen | P0 | Offen | Identity-/Tenant-Entscheidung |
| Vertical Slice gegen reale PostgreSQL-Instanz inklusive Migration, Seed und HTTP/UI-Smoke-Test ausführen | P0 | Offen | lokale/CI PostgreSQL-Umgebung |

## P1

| Beschreibung | Priorität | Status | Abhängigkeiten |
|---|---:|---|---|
| API-Integrationstests mit Testcontainer für Imports und Data Products | P1 | Offen | Container-Infrastruktur |
| React-Teststack und Tests für Availability, Generate und Versionenauswahl | P1 | Offen | Vitest/Testing Library Entscheidung |
| Concurrency-Strategie für DataProduct-Versionsnummern ergänzen | P1 | Offen | Transaktions-/Retry-Konzept |
| Meter-/Building-DuplicateValidator implementieren | P1 | Offen | fachliche Identitätsregeln |
| Haupt-/Unterzählerhierarchie modellieren und Doppelzählung verhindern | P1 | Offen | Metering-Fachentscheidung |
| ProblemDetails zentral auf fachliche Exception-Typen abbilden | P1 | Offen | Application-Fehlerkonzept |

## P2

| Beschreibung | Priorität | Status | Abhängigkeiten |
|---|---:|---|---|
| Unbenutzte DataProduct-Platzhalter (`GenerateDataProductRequest`, GenerationResult, AvailabilityRequest, Validator) entscheiden | P2 | Offen | stabiler Application-Vertrag |
| Überlappende Excel-Abstraktionen und Writer fachlich bereinigen | P2 | Offen | Export-/Import-Abgrenzung |
| Silver-/Gold-Zonen als technische Persistenzpfade konkretisieren | P2 | Offen | Data-Platform-Zielarchitektur |
| Kunden-, Gebäude- und Analytics-Webseiten produktiv anbinden | P2 | Offen | entsprechende Query APIs |
| Marketplace-Publication vollständig konfigurieren oder aus RC-Scope entfernen | P2 | Offen | Marketplace-Roadmap |

## P3

| Beschreibung | Priorität | Status | Abhängigkeiten |
|---|---:|---|---|
| Mobility-, Subscription- und Aggregation-Gerüste implementieren | P3 | Geplant | spätere Produktphasen |
| Mandantenfähigkeit und frei definierte Analysegruppen | P3 | Geplant | Tenant-/Authorization-Konzept |
| TimescaleDB/Partitionierung für große Messwertmengen evaluieren | P3 | Geplant | Last- und Mengentests |
