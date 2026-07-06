using ClosedXML.Excel;
using Enset.Application.Imports.DTOs;

namespace Enset.Infrastructure.Imports.Excel;

public sealed class ExcelWorkbookWriter : IExcelWorkbookWriter
{
    private const string WorksheetName = "Customers";
    private const string TableName = "Customer";
    private const string CustomerIdColumnName = "InternalCustomerId";

    private readonly string _filePath;

    public ExcelWorkbookWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void UpdateCustomers(IEnumerable<CustomerImportDto> customers)
    {
        using var workbook = new XLWorkbook(_filePath);
        var table = workbook.Worksheet(WorksheetName).Table(TableName);

        var columns = table.Fields.ToDictionary(
            field => field.Name,
            field => field.Index + 1,
            StringComparer.OrdinalIgnoreCase);

        var customerIdColumn = GetRequiredColumn(columns, CustomerIdColumnName);

        var customersById = customers
            .Where(customer => !string.IsNullOrWhiteSpace(customer.ExternalCustomerId))
            .GroupBy(customer => customer.ExternalCustomerId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        foreach (var row in table.DataRange.Rows())
        {
            var customerId = row.Cell(customerIdColumn).GetFormattedString().Trim();

            if (!customersById.TryGetValue(customerId, out var customer))
                continue;

            SetCell(row, columns, "OrganizationName", customer.CompanyName);
            SetCell(row, columns, "Street", customer.Street);
            SetCell(row, columns, "HouseNumber", customer.HouseNumber);
            SetCell(row, columns, "PostalCode", customer.PostalCode);
            SetCell(row, columns, "City", customer.City);
            SetCell(row, columns, "Email", customer.Email);
            SetCell(row, columns, "PhoneNumber", customer.Phone);
        }

        workbook.Save();
    }

    private static int GetRequiredColumn(
        IReadOnlyDictionary<string, int> columns,
        string columnName)
    {
        if (columns.TryGetValue(columnName, out var columnIndex))
            return columnIndex;

        throw new InvalidOperationException(
            $"Column '{columnName}' not found in table '{TableName}'.");
    }

    private static void SetCell(
        IXLRangeRow row,
        IReadOnlyDictionary<string, int> columns,
        string columnName,
        string? value)
    {
        if (columns.TryGetValue(columnName, out var columnIndex))
            row.Cell(columnIndex).Value = value ?? string.Empty;
    }
}
