##Anonymisierung aller Kunden:
    -Zählpunktnummer nie im Klartext für Analyse speichern, aber deterministisch pseudonymisieren.
    Bsp:
        MeterNumber: AT001234...
        ↓
        HMACSHA256(secret, normalizedMeterNumber)
        ↓
        MeterPrivacyKey

