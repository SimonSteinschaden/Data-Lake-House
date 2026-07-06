using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Validation;

public class ExcelImportValidator : IImportValidator
{
    public ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings)
    {
        var report = new ImportReport
        {
            CustomerCount = customers.Count,
            BuildingCount = buildings.Count
        };

        ValidateCustomers(customers, report.Issues);
        ValidateBuildings(buildings, report.Issues);
        ValidateCustomerBuildingRelations(customers, buildings, report.Issues);

        return report;
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