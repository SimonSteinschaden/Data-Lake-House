using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Validation;

public class ExcelImportValidator : IImportValidator
{
    public ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> meterReadings)
    {
        var report = new ImportReport
        {
            CustomerCount = customers.Count,
            BuildingCount = buildings.Count,
            MeterCount = meters.Count,
            MeterReadingCount = meterReadings.Count
        };

        ValidateCustomers(customers, report.Issues);
        ValidateBuildings(buildings, report.Issues);
        ValidateCustomerBuildingRelations(customers, buildings, report.Issues);
        ValidateMeters(customers, buildings, meters, report.Issues);
        ValidateMeterReadings(meters, meterReadings, report.Issues);

        return report;
    }

    private static void ValidateMeters(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        ICollection<ImportIssue> issues)
    {
        var customerIds = customers
            .Select(customer => customer.InternalCustomerId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buildingIds = buildings
            .Select(building => building.InternalBuildingId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var meter in meters)
        {
            if (string.IsNullOrWhiteSpace(meter.MeterNumber))
            {
                AddIssue(issues, ImportIssueType.InvalidMeterNumber,
                    $"Meter row {meter.RowNumber}: MeterNumber is empty.");
            }

            if (!string.IsNullOrWhiteSpace(meter.ExternalCustomerId) &&
                !customerIds.Contains(meter.ExternalCustomerId.Trim()))
            {
                AddIssue(issues, ImportIssueType.MissingCustomer,
                    $"Meter row {meter.RowNumber}: references unknown customer '{meter.ExternalCustomerId}'.");
            }

            if (!string.IsNullOrWhiteSpace(meter.ExternalBuildingId) &&
                !buildingIds.Contains(meter.ExternalBuildingId.Trim()))
            {
                AddIssue(issues, ImportIssueType.MissingBuilding,
                    $"Meter row {meter.RowNumber}: references unknown building '{meter.ExternalBuildingId}'.");
            }
        }

        foreach (var duplicate in meters
            .Where(meter => !string.IsNullOrWhiteSpace(meter.MeterNumber))
            .GroupBy(meter => meter.MeterNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            AddIssue(issues, ImportIssueType.DuplicateMeter,
                $"Duplicate MeterNumber '{duplicate.Key}' found in rows: {string.Join(", ", duplicate.Select(row => row.RowNumber))}.",
                requiresUserDecision: true);
        }
    }

    private static void ValidateMeterReadings(
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> readings,
        ICollection<ImportIssue> issues)
    {
        var meterNumbers = meters
            .Select(meter => meter.MeterNumber?.Trim())
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var reading in readings)
        {
            if (string.IsNullOrWhiteSpace(reading.MeterNumber) ||
                !meterNumbers.Contains(reading.MeterNumber.Trim()))
            {
                AddIssue(issues, ImportIssueType.MissingMeter,
                    $"MeterReading row {reading.RowNumber}: references unknown MeterNumber '{reading.MeterNumber}'.");
            }

            var mapped = MeterReadingExcelRowMapper.ToDto(reading);
            if (!mapped.HasError)
                continue;

            var issueType = mapped.ErrorMessage?.Contains("Timestamp", StringComparison.Ordinal) == true
                ? ImportIssueType.InvalidTimestamp
                : ImportIssueType.InvalidValue;
            AddIssue(issues, issueType,
                $"MeterReading row {reading.RowNumber}: {mapped.ErrorMessage}.");
        }
    }

    private static void AddIssue(
        ICollection<ImportIssue> issues,
        ImportIssueType type,
        string message,
        bool requiresUserDecision = false)
    {
        issues.Add(new ImportIssue
        {
            Type = type,
            Severity = ImportIssueSeverity.Error,
            Message = message,
            RequiresUserDecision = requiresUserDecision
        });
    }

    private static void ValidateCustomers(
        IReadOnlyList<CustomerExcelRow> customers,
        ICollection<ImportIssue> issues)
    {
        if (customers.Count == 0)
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = "No customers found.",
                RequiresUserDecision = false
            });
        }

        foreach (var customer in customers.Where(c => string.IsNullOrWhiteSpace(c.InternalCustomerId)))
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = $"Customer row {customer.RowNumber}: InternalCustomerId is empty.",
                RequiresUserDecision = false
            });
        }

        var duplicateIds = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .GroupBy(c => c.InternalCustomerId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(c => c.RowNumber));

            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.DuplicateCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = $"Duplicate CustomerID '{group.Key}' found in rows: {rows}.",
                RequiresUserDecision = true
            });
        }
    }

    private static void ValidateBuildings(
        IReadOnlyList<BuildingExcelRow> buildings,
        ICollection<ImportIssue> issues)
    {
        if (buildings.Count == 0)
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingBuilding,
                Severity = ImportIssueSeverity.Error,
                Message = "No buildings found.",
                RequiresUserDecision = false
            });
        }

        foreach (var building in buildings)
        {
            if (string.IsNullOrWhiteSpace(building.InternalBuildingId))
            {
                issues.Add(new ImportIssue
                {
                    Type = ImportIssueType.MissingBuilding,
                    Severity = ImportIssueSeverity.Error,
                    Message = $"Building row {building.RowNumber}: InternalBuildingId is empty.",
                    RequiresUserDecision = false
                });
            }

            if (string.IsNullOrWhiteSpace(building.InternalCustomerId))
            {
                issues.Add(new ImportIssue
                {
                    Type = ImportIssueType.MissingCustomer,
                    Severity = ImportIssueSeverity.Error,
                    Message = $"Building row {building.RowNumber}: InternalCustomerId is empty.",
                    RequiresUserDecision = false
                });
            }
        }

        var duplicateIds = buildings
            .Where(b => !string.IsNullOrWhiteSpace(b.InternalBuildingId))
            .GroupBy(b => b.InternalBuildingId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(b => b.RowNumber));

            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.DuplicateBuilding,
                Severity = ImportIssueSeverity.Error,
                Message = $"Duplicate BuildingID '{group.Key}' found in rows: {rows}.",
                RequiresUserDecision = true
            });
        }
    }

    private static void ValidateCustomerBuildingRelations(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        ICollection<ImportIssue> issues)
    {
        var validCustomerIds = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .Select(c => c.InternalCustomerId!.Trim())
            .ToHashSet();

        foreach (var building in buildings)
        {
            if (string.IsNullOrWhiteSpace(building.InternalCustomerId))
                continue;

            var customerId = building.InternalCustomerId.Trim();

            if (!validCustomerIds.Contains(customerId))
            {
                issues.Add(new ImportIssue
                {
                    Type = ImportIssueType.MissingCustomer,
                    Severity = ImportIssueSeverity.Error,
                    Message = $"Building row {building.RowNumber}: references unknown CustomerID '{customerId}'.",
                    RequiresUserDecision = false
                });
            }
        }
    }
}
