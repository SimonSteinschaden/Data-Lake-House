using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Models;
using Enset.Application.CanonicalSnapshots;

namespace Enset.Application.Exports.LEB.Validation;

public enum ValidationSeverity { Error, Warning }
public sealed record ValidationError(
    string Code, string Table, string? RowId, string Field, string Message,
    ValidationSeverity Severity = ValidationSeverity.Error);
public sealed record ValidationWarning(
    string Code, string Table, string? RowId, string Field, string Message,
    ValidationSeverity Severity = ValidationSeverity.Warning);
public sealed record ValidationResult(
    bool CanExport, IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings)
{
    public int DatasetCount { get; init; }
    public int SuitableCount { get; init; }
    public int NotSuitableCount { get; init; }
    public int WarningCount => Warnings.Count;
    public int BlockingErrorCount => Errors.Count;
}

public sealed class LebExportValidator
{
    public ValidationResult Validate(LebExportDataset dataset)
    {
        var result = Validate(dataset.Contract);
        var errors = result.Errors.ToList();
        foreach (var item in dataset.Assessments.Where(x =>
                     x.LebSuitability == SuitabilityStatus.NotSuitable))
            errors.Add(new(
                "LEB_NOT_SUITABLE",
                item.EntityType,
                item.EntityId.ToString(),
                "LebSuitability",
                $"{item.FachlicheIdentifikation}: Für LEB nicht geeignet."));
        var suitable = dataset.Assessments.Count(x =>
            x.LebSuitability == SuitabilityStatus.Suitable);
        var notSuitable = dataset.Assessments.Count - suitable;
        return new(errors.Count == 0, errors, result.Warnings)
        {
            DatasetCount = dataset.Assessments.Count,
            SuitableCount = suitable,
            NotSuitableCount = notSuitable
        };
    }

    public ValidationResult Validate(NoeLebExportContractV1 contract)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        foreach (var row in contract.Municipalities)
            Required(errors, row.MunicipalityNumber, "MUNICIPALITY_NUMBER",
                "Municipalities", row.MunicipalityId, "MunicipalityNumber",
                "Gemeindenummer fehlt.");
        foreach (var row in contract.Objects)
        {
            if (row.MunicipalityId is null)
                Error(errors, "MUNICIPALITY_NUMBER", "Objects", row.ObjectId,
                    "MunicipalityId", "Gemeinde oder Gemeindenummer fehlt.");
            Required(errors, row.ObjectCode, "OBJECT_CODE", "Objects", row.ObjectId,
                "ObjectCode", "Objektcode fehlt.");
            Required(errors, row.UsageType, "USAGE", "Objects", row.ObjectId,
                "UsageType", "Nutzung fehlt.");
            if (row.ConditionedGrossFloorArea is null or <= 0)
                Error(errors, "CONDITIONED_AREA", "Objects", row.ObjectId,
                    "ConditionedGrossFloorArea", "Konditionierte Fläche fehlt.");
            Warn(warnings, row.ConstructionYear is null, "CONSTRUCTION_YEAR", "Objects",
                row.ObjectId, "ConstructionYear", "Baujahr fehlt.");
            Warn(warnings, row.FloorCount is null, "FLOOR_COUNT", "Objects",
                row.ObjectId, "FloorCount", "Geschoßanzahl fehlt.");
            Warn(warnings, row.ConditionedGrossVolume is null, "VOLUME", "Objects",
                row.ObjectId, "ConditionedGrossVolume", "Volumen fehlt.");
            Warn(warnings, string.IsNullOrWhiteSpace(row.ContactName), "CONTACT", "Objects",
                row.ObjectId, "ContactName", "Ansprechpartner fehlt.");
            Warn(warnings, row.ReferenceMetricValue is null, "REFERENCE_METRIC",
                "Objects", row.ObjectId, "ReferenceMetricValue", "Referenzgröße fehlt.");
        }
        foreach (var row in contract.Meters)
        {
            if (row.ObjectId is null) Error(errors, "METER_OBJECT", "Meters", row.MeterId,
                "ObjectId", "Zähler ist keinem Objekt zugeordnet.");
            Required(errors, row.EnergyCarrier, "METER_MEDIUM", "Meters", row.MeterId,
                "EnergyCarrier", "Zählermedium fehlt.");
            Required(errors, row.Unit, "METER_UNIT", "Meters", row.MeterId,
                "Unit", "Zählereinheit fehlt.");
            Required(errors, row.MeterCategory, "METER_CATEGORY", "Meters", row.MeterId,
                "MeterCategory", "Zählerkategorie fehlt.");
            Required(errors, row.NoeNavigatorMedium, "NAVIGATOR_MEDIUM", "Meters",
                row.MeterId, "NoeNavigatorMedium", "Navigator-Medium ist unbekannt.");
            Warn(warnings, string.IsNullOrWhiteSpace(row.GridMeteringPointNumber),
                "GRID_METERING_POINT", "Meters", row.MeterId,
                "GridMeteringPointNumber", "Zählpunktnummer fehlt.");
            var monthCount = contract.Readings.Where(x => x.MeterId == row.MeterId &&
                    x.ReadingTimestamp.HasValue)
                .Select(x => new
                {
                    x.ReadingTimestamp!.Value.Year,
                    x.ReadingTimestamp.Value.Month
                }).Distinct().Count();
            Warn(warnings, monthCount is > 0 and < 12, "LESS_THAN_12_MONTHS",
                "Meters", row.MeterId, "Readings",
                "Weniger als zwölf Monate mit Messwerten vorhanden.");
        }
        foreach (var row in contract.Readings)
        {
            if (row.ReadingTimestamp is null) Error(errors, "READING_TIMESTAMP",
                "Readings", row.MeterId, "ReadingTimestamp", "Messwertzeitpunkt fehlt.");
            if (row.ReadingValue is null) Error(errors, "READING_VALUE",
                "Readings", row.MeterId, "ReadingValue", "Messwert fehlt.");
        }
        foreach (var row in contract.EnergySystems)
        {
            var purpose = row.SupplyPurpose ?? string.Empty;
            Warn(warnings, row.InstalledCapacity is null &&
                purpose.Contains("Photovoltaic", StringComparison.OrdinalIgnoreCase),
                "PV_CAPACITY", "EnergySystems", row.EnergySystemId,
                "InstalledCapacity", "PV-Leistung fehlt.");
            Warn(warnings, row.InstalledCapacity is null &&
                (purpose.Contains("Heating", StringComparison.OrdinalIgnoreCase) ||
                 purpose.Contains("HeatPump", StringComparison.OrdinalIgnoreCase) ||
                 purpose.Contains("DistrictHeating", StringComparison.OrdinalIgnoreCase) ||
                 purpose.Contains("Boiler", StringComparison.OrdinalIgnoreCase)),
                "HEATING_CAPACITY", "EnergySystems", row.EnergySystemId,
                "InstalledCapacity", "Heizleistung fehlt.");
        }
        return new(errors.Count == 0, errors, warnings);
    }

    private static void Required(List<ValidationError> values, string? value,
        string code, string table, Guid id, string field, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) Error(values, code, table, id, field, message);
    }
    private static void Error(List<ValidationError> values, string code, string table,
        Guid id, string field, string message) =>
        values.Add(new(code, table, id.ToString(), field, message));
    private static void Warn(List<ValidationWarning> values, bool condition, string code,
        string table, Guid id, string field, string message)
    {
        if (condition) values.Add(new(code, table, id.ToString(), field, message));
    }
}
