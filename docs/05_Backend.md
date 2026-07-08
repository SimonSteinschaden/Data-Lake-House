# Backend- und Entwicklerdokumentation

## Projekte

| Projekt | Verantwortung | Abhängigkeiten |
|---|---|---|
| `Enset.Domain` | Entities, Enums und fachliche Basistypen | keine |
| `Enset.Application` | Use Cases, Ports, DTOs, Importworkflow und WriteGate | Domain |
| `Enset.Infrastructure` | EF Core, Npgsql, ClosedXML und konkrete Adapter | Domain, Application |
| `Enset.Worker` | Composition Root und Konsolenausgabe | alle Bibliotheken |
| `Enset.Api` | REST-Endpunkte und API-Composition-Root | Application, Infrastructure |
| `Enset.Import.Tests` | Architektur- und Workflowtests | API, Application, Infrastructure |

Alle Projekte verwenden `net10.0` mit aktivierten Nullable Reference Types und Implicit Usings.

## Application-Struktur

- `Imports/Abstractions`: Ports für Coordinator, Reader, Mapper, Validator, Resolution, WriteGate, Writer und Logger
- `Imports/Coordination`: Analyze-only-`ImportCoordinator`
- `Imports/DTOs`: Import- und vorbereitende API-DTOs
- `Imports/Validation`: aktive Excel-Validierung und teilweise leere Validator-Platzhalter
- `Imports/DuplicationCheck`: interne Kandidaten, Identity Builder, Mapping, Services und Resolution-Hilfen
- `Imports/Issues`: zentrales Issue- und ResolutionAction-Modell
- `Imports/Reports`: `ImportReport` und `AutoFixReport`
- `Imports/Decisions`: technische Importentscheidung
- `Imports/WriteGate`: `ImportWriteContext` und Gate-Implementierung

## Infrastructure-Struktur

- `DBContext.cs`: EF-Core-Context
- `Persistence/` und `Migrations/`: Design-Time-Factory und Migrationen
- `Imports/Excel/`: ExcelImportReader/-Writer und Workbook-Adapter
- `Imports/Csv/`: MeterReading-CSV-Reader
- `Imports/Factories/`, `Mappings/`, `Services/`: weitere Importbausteine
- `Exports/Excel/`: vorhandener Excel-Exportadapter

ClosedXML ist ausschließlich im Infrastructure-Projekt als Package referenziert.

## EnsetDbContext

Aktive DbSets:

- `Customers`, `Projects`, `Buildings`
- `EnergySystems`, `Meters`, `MeterReadings`
- `Documents`
- `CalculationResults`, `BenchmarkDatasets`

Konfiguration:

- Composite Key `MeterReading.MeterId + Timestamp`;
- Index auf `MeterReading.Timestamp`;
- Unique Index auf `Meter.MeterNumber`;
- Beziehung `Meter -> MeterReadings` über `MeterId`.

`ImportJob` und `DataSource` sind modelliert, aber nicht als aktive DbSets registriert.

## Worker ausführen

Der Worker verwendet aktuell einen hart codierten lokalen XLSM-Pfad in `Program.cs`. Der produktive Argumentpfad ist dort als TODO auskommentiert. Der Lauf analysiert und protokolliert nur; er erzeugt keine Ausgabedatei.

Build:

```powershell
dotnet build src/Enset.Worker/Enset.Worker.csproj --no-restore
```

Aktueller Nachweis: erfolgreich, 0 Warnungen, 0 Fehler.

## API und Tests

```powershell
dotnet build src/Enset.Api/Enset.Api.csproj --no-restore
dotnet test tests/Enset.Import.Tests/Enset.Import.Tests.csproj --no-restore
```

Aktueller Nachweis: API-Build erfolgreich; 7 von 7 Tests bestanden.

## Bekannte Entwicklerlücken

- keine Solution-Datei;
- kein Generic Host und keine Dependency-Injection-Konfiguration;
- hart codierter Entwicklungsdateipfad;
- mehrere leere Validator-/Mapper-/AutoFix-Platzhalter;
- ältere bereits versionierte `bin`-/`obj`-Artefakte; neue Artefakte werden durch `.gitignore` ausgeschlossen;
- uneinheitliche Formatierung und teilweise ältere Encoding-Artefakte im Quellcode;
- keine CI/CD- oder Docker-Konfiguration.
