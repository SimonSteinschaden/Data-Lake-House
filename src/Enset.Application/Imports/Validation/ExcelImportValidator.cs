using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Validation;

/*TODO: Erweiterung um Prüfungen wie:
Customer ohne Telefon/Email
Building ohne Adresse
Building ohne Baujahr
Building ohne Nutzfläche
Customer ohne Building
 -> in zugehörige Classes implementieren*/


public static class ExcelImportValidator
{
    public static ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings)
    {
        var report = new ImportReport
        {
            CustomerCount = customers.Count,
            BuildingCount = buildings.Count
        };

        ValidateCustomers(customers, report.Errors);
        ValidateBuildings(buildings, report.Errors);
        ValidateCustomerBuildingRelations(customers, buildings, report.Errors);

        return report;
    }

    private static void ValidateCustomers(
        IReadOnlyList<CustomerExcelRow> customers,
        List<string> errors)
    {
        if (customers.Count == 0)
            errors.Add("No customers found.");

        var emptyIds = customers
            .Where(c => string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .ToList();

        foreach (var customer in emptyIds)
        {
            errors.Add($"Customer row {customer.RowNumber}: InternalCustomerId is empty.");
        }

        var duplicateIds = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .GroupBy(c => c.InternalCustomerId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(c => c.RowNumber));
            errors.Add($"Duplicate CustomerID '{group.Key}' found in rows: {rows}.");
        }
    }

    private static void ValidateBuildings(
        IReadOnlyList<BuildingExcelRow> buildings,
        List<string> errors)
    {
        if (buildings.Count == 0)
            errors.Add("No buildings found.");

        foreach (var building in buildings)
        {
            if (string.IsNullOrWhiteSpace(building.InternalBuildingId))
                errors.Add($"Building row {building.RowNumber}: InternalBuildingId is empty.");

            if (string.IsNullOrWhiteSpace(building.InternalCustomerId))
                errors.Add($"Building row {building.RowNumber}: InternalCustomerId is empty.");
        }

        var duplicateIds = buildings
            .Where(b => !string.IsNullOrWhiteSpace(b.InternalBuildingId))
            .GroupBy(b => b.InternalBuildingId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(b => b.RowNumber));
            errors.Add($"Duplicate BuildingID '{group.Key}' found in rows: {rows}.");
        }
    }

    private static void ValidateCustomerBuildingRelations(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        List<string> errors)
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
                errors.Add(
                    $"Building row {building.RowNumber}: references unknown CustomerID '{customerId}'.");
            }
        }
    }
}