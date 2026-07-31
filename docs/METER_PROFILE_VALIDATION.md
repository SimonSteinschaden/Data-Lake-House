# Validierung von Messprofilen

Ein Profil ohne technische Analyse ist Bronze. Eine abgeschlossene Analyse
mit offenen Problemen ist Silver. Gold setzt den Status `Confirmed`, keine
blockierenden Probleme und abgeschlossene fachliche Kuration voraus.

Das versionierte Analyseergebnis enthält Zeitraum, Soll-/Ist-Anzahl,
Vollständigkeit, Lücken, Anomalien, Blocker, Warnungen, Version, Ausführenden
und Detailprobleme. Zu prüfen sind insbesondere fehlende oder doppelte
Zeitstempel, Intervalle, Einheiten, negative/unplausible Werte, Nullserien,
Sprünge, Ausreißer und fehlende Perioden.

Ersatzwerte dürfen nie stillschweigend entstehen. Methode, Referenz,
Confidence, Urheber, Begründung und QualityFlag müssen auditierbar sein.
Unbestätigte Ersatzwerte können kein Gold ermöglichen.

Analyseversionen, Issues und Entscheidungen bleiben unveränderlich
historisiert. Eine fehlgeschlagene oder abgebrochene Analyse verdrängt keine
bereits aktuelle bestätigte Analyse.

Neue oder geänderte Messwerte markieren die aktuelle Analyse als veraltet.
Bis zu einer neuen Analyse bleibt der Zählpunkt operativ Bronze.
