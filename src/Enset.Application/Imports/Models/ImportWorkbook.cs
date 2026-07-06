namespace Enset.Application.Imports.Models;

public sealed class ImportWorkbook
{
    public IReadOnlyList<CustomerExcelRow> Customers { get; init; } = [];
    public IReadOnlyList<BuildingExcelRow> Buildings { get; init; } = [];
}
