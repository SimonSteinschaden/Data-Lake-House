using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Models;

namespace Enset.Infrastructure.Exports.LEB;

public sealed class ExcelLebExporter : IExcelLebExporter
{
    public LebExportFile Export(NoeLebExportContractV1 contract)
    {
        using var workbook = new XLWorkbook();
        Add(workbook, "Municipalities", contract.Municipalities);
        Add(workbook, "Objects", contract.Objects);
        Add(workbook, "Meters", contract.Meters);
        Add(workbook, "Readings", contract.Readings);
        Add(workbook, "EnergySystems", contract.EnergySystems);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new(output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"NoeLebExport_{contract.ExportTimestamp:yyyyMMdd_HHmmss}.xlsx");
    }

    private static void Add<T>(XLWorkbook workbook, string name, IReadOnlyList<T> rows)
    {
        var sheet = workbook.Worksheets.Add(name);
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        for (var column = 0; column < properties.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = properties[column].Name;
            sheet.Cell(1, column + 1).Style.Font.Bold = true;
        }
        for (var row = 0; row < rows.Count; row++)
        for (var column = 0; column < properties.Length; column++)
            Set(sheet.Cell(row + 2, column + 1), properties[column].GetValue(rows[row]));
        sheet.SheetView.FreezeRows(1);
        sheet.ColumnsUsed().AdjustToContents(1, Math.Min(rows.Count + 1, 100));
        if (rows.Count > 0)
            sheet.Range(1, 1, rows.Count + 1, properties.Length)
                .CreateTable($"Leb{name}");
    }

    private static void Set(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null: cell.Value = string.Empty; break;
            case DateTime timestamp:
                cell.Value = timestamp;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case decimal number: cell.Value = number; break;
            case int number: cell.Value = number; break;
            case bool boolean: cell.Value = boolean; break;
            default: cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; break;
        }
    }
}
