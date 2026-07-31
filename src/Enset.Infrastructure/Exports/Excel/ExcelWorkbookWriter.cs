using ClosedXML.Excel;
using Enset.Application.Exports.Abstractions;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Exports.Excel;

public class ExcelWorkbookWriter : IExcelWriter
{
    private const string CustomersSheetName = "Customers";
    private const string CustomersTableName = "Customer";
    private const string BuildingsSheetName = "Buildings";
    private const string BuildingsTableName = "Tabelle2"; //TODO: Tabellenname anpassen im Excel. Ordentliche Benennung.

    private const string CustomerIdColumnName = "InternalCustomerId";
    private const string BuildingIdColumnName = "InternalBuildingId";
    private const string NotesColumnName = "Notes";

    public void UpdateWorkbook(
        string sourceFilePath,
        string targetFilePath,
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings)
    {
        using var workbook = OpenWorkbookSnapshot(sourceFilePath);

        workbook.SaveAs(targetFilePath);
    }

    public bool UpdateCustomerId(
        string sourceFilePath,
        string targetFilePath,
        int rowNumber,
        string newCustomerId)
    {
        using var workbook = OpenWorkbookSnapshot(sourceFilePath);

        var table = GetTable(
            workbook,
            CustomersSheetName,
            CustomersTableName);

        var success = UpdateTableCellByRowNumber(
            table,
            rowNumber,
            CustomerIdColumnName,
            newCustomerId);

        SaveIfChanged(workbook, targetFilePath, success);

        return success;
    }

    public bool UpdateCustomerNotes(
        string sourceFilePath,
        string targetFilePath,
        string internalCustomerId,
        string notes)
    {
        return UpdateCustomerField(
            sourceFilePath,
            targetFilePath,
            internalCustomerId,
            NotesColumnName,
            notes);
    }

    public bool UpdateBuildingNotes(
        string sourceFilePath,
        string targetFilePath,
        string internalBuildingId,
        string notes)
    {
        return UpdateBuildingField(
            sourceFilePath,
            targetFilePath,
            internalBuildingId,
            NotesColumnName,
            notes);
    }

    public bool UpdateCustomerField(
        string sourceFilePath,
        string targetFilePath,
        string customerId,
        string columnName,
        string value)
    {
        using var workbook = OpenWorkbookSnapshot(sourceFilePath);

        var table = GetTable(
            workbook,
            CustomersSheetName,
            CustomersTableName);

        var success = UpdateTableCell(
            table,
            CustomerIdColumnName,
            customerId,
            columnName,
            value);

        SaveIfChanged(workbook, targetFilePath, success);

        return success;
    }

    public bool UpdateBuildingField(
        string sourceFilePath,
        string targetFilePath,
        string buildingId,
        string columnName,
        string value)
    {
        using var workbook = OpenWorkbookSnapshot(sourceFilePath);

        var table = GetTable(
            workbook,
            BuildingsSheetName,
            BuildingsTableName);

        var success = UpdateTableCell(
            table,
            BuildingIdColumnName,
            buildingId,
            columnName,
            value);

        SaveIfChanged(workbook, targetFilePath, success);

        return success;
    }

    public bool UpdateBuildingId(
    string sourceFilePath,
    string targetFilePath,
    int rowNumber,
    string newBuildingId)
    {
        using var workbook = OpenWorkbookSnapshot(sourceFilePath);

        var table = GetTable(
            workbook,
            BuildingsSheetName,
            BuildingsTableName);

        var success = UpdateTableCellByRowNumber(
            table,
            rowNumber,
            BuildingIdColumnName,
            newBuildingId);

        SaveIfChanged(workbook, targetFilePath, success);

        return success;
    }

    private static XLWorkbook OpenWorkbookSnapshot(string filePath)
    {
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var memoryStream = new MemoryStream();

        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return new XLWorkbook(memoryStream);
    }

    private static IXLTable GetTable(
        XLWorkbook workbook,
        string worksheetName,
        string tableName)
    {
        var worksheet = workbook.Worksheet(worksheetName);
        return worksheet.Table(tableName);
    }

    private static bool UpdateTableCell(
        IXLTable table,
        string keyColumnName,
        string keyValue,
        string targetColumnName,
        string value)
    {
        var keyColumnIndex = GetRequiredColumnIndex(table, keyColumnName);
        var targetColumnIndex = GetRequiredColumnIndex(table, targetColumnName);

        foreach (var row in table.DataRange.Rows())
        {
            var currentKeyValue = row.Cell(keyColumnIndex)
                .GetFormattedString()
                .Trim();

            if (currentKeyValue != keyValue)
                continue;

            row.Cell(targetColumnIndex).Value = value;

            return true;
        }

        return false;
    }

    private static bool UpdateTableCellByRowNumber(
        IXLTable table,
        int rowNumber,
        string targetColumnName,
        string value)
    {
        var targetColumnIndex = GetRequiredColumnIndex(table, targetColumnName);

        var row = table.DataRange.Rows()
            .FirstOrDefault(r => r.RowNumber() == rowNumber);

        if (row is null)
            return false;

        row.Cell(targetColumnIndex).Value = value;

        return true;
    }

    private static int GetRequiredColumnIndex(
        IXLTable table,
        string columnName)
    {
        var field = table.Fields
            .FirstOrDefault(f => f.Name == columnName);

        if (field is null)
            throw new InvalidOperationException($"Column '{columnName}' not found.");

        return field.Index + 1;
    }

    private static void SaveIfChanged(
        XLWorkbook workbook,
        string targetFilePath,
        bool hasChanged)
    {
        if (hasChanged)
            workbook.SaveAs(targetFilePath);
    }
}