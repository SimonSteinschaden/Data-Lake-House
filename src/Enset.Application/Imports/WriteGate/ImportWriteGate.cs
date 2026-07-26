using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.WriteGate;

public sealed class ImportWriteGate : IImportWriteGate
{
    public ImportWriteGateResult Evaluate(ImportWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<string>();
        var report = context.Report;

        if (report is null)
        {
            errors.Add("Import report is required.");
            return new ImportWriteGateResult { Errors = errors };
        }

        if (context.ImportId == Guid.Empty || context.ImportId != report.ImportId)
            errors.Add("Import id does not match the report.");

        if (string.IsNullOrWhiteSpace(context.UserId))
            errors.Add("User context is required.");

        if (report.Status != ImportStatus.ReadyToCommit)
            errors.Add($"Import status '{report.Status}' is not commit-ready.");

        if (report.Decision.Type == ImportDecisionType.Abort)
            errors.Add("Import decision is Abort.");

        if (context.TargetWriter == ImportWriterType.Excel &&
            string.IsNullOrWhiteSpace(context.TargetLocation))
        {
            errors.Add("An Excel target location is required.");
        }

        if (report.HasOpenCommitBlockingIssues)
        {
            errors.Add("At least one commit-blocking issue is unresolved.");
        }

        if (context.TargetWriter == ImportWriterType.Database)
            ValidateDatabaseReferences(context, errors);

        return new ImportWriteGateResult { Errors = errors };
    }

    public bool CanWrite(ImportWriteContext context)
    {
        return Evaluate(context).IsAllowed;
    }

    private static void ValidateDatabaseReferences(
        ImportWriteContext context,
        ICollection<string> errors)
    {
        var customerIds = context.Customers
            .Select(customer => customer.ExternalCustomerId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buildingIds = context.Buildings
            .Select(building => building.ExternalBuildingId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var meterNumbers = context.Meters
            .Select(meter => meter.MeterNumber?.Trim())
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Select(number => number!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (context.Customers.Any(customer =>
                string.IsNullOrWhiteSpace(customer.ExternalCustomerId)))
            errors.Add("Every customer requires ExternalCustomerId.");

        if (context.Buildings.Any(building =>
                string.IsNullOrWhiteSpace(building.ExternalBuildingId)))
            errors.Add("Every building requires ExternalBuildingId.");

        if (context.Buildings.Any(building =>
                string.IsNullOrWhiteSpace(building.ExternalCustomerId) ||
                !customerIds.Contains(building.ExternalCustomerId.Trim())))
            errors.Add(
                "Every building requires an existing ExternalCustomerId.");

        if (context.Meters.Any(meter =>
                string.IsNullOrWhiteSpace(meter.MeterNumber)))
            errors.Add("Every meter requires MeterNumber.");

        if (context.Meters.Any(meter =>
                !meter.AllowUnassignedBuilding &&
                (string.IsNullOrWhiteSpace(meter.ExternalBuildingId) ||
                 !buildingIds.Contains(meter.ExternalBuildingId.Trim()))))
            errors.Add(
                "Every meter requires an existing ExternalBuildingId.");

        var hasOpenMeterAssignment = context.Report?.Issues.Any(issue =>
            issue.Type == ImportIssueType.AssignMeterRequired &&
            !issue.IsResolved) == true;
        if (!hasOpenMeterAssignment &&
            context.MeterReadings.Any(reading =>
                !reading.MeterId.HasValue &&
                (string.IsNullOrWhiteSpace(reading.MeterNumber) ||
                 !meterNumbers.Contains(reading.MeterNumber.Trim()))))
            errors.Add(
                "Every meter reading requires an existing MeterNumber.");
    }
}
