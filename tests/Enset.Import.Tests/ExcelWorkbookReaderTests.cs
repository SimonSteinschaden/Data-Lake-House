using Enset.Infrastructure.Imports.Excel;
using ClosedXML.Excel;
using Enset.Application.Imports.Exceptions;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ExcelWorkbookReaderTests
{
    [Fact]
    public void Read_ExistingMvpWorkbook_ReturnsImportData()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "ENSET_Testimport_MVP.xlsx");

        using var stream = File.OpenRead(filePath);

        var result = new ExcelWorkbookReader().Read(stream);

        Assert.Single(result.Customers);
        Assert.Single(result.Buildings);
        Assert.Single(result.Meters);
        Assert.Equal(5, result.MeterReadings.Count);
    }

    [Fact]
    public void Read_LegacyTableWorkbook_RemainsSupported()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Datenbasis Grundlage RDW.xlsm");

        using var stream = File.OpenRead(filePath);

        var result = new ExcelWorkbookReader().Read(stream);

        Assert.NotEmpty(result.Customers);
        Assert.NotEmpty(result.Buildings);
        Assert.Empty(result.Meters);
        Assert.Empty(result.MeterReadings);
    }

    [Fact]
    public void Read_MissingWorksheet_ThrowsDescriptiveImportException()
    {
        using var stream = CreateWorkbook(("Customers", ["ExternalCustomerId", "CompanyName"]));

        var exception = Assert.Throws<InvalidImportFileException>(
            () => new ExcelWorkbookReader().Read(stream));

        Assert.Equal("Worksheet 'Buildings' is missing.", exception.Message);
    }

    [Fact]
    public void Read_MissingRequiredColumn_ThrowsDescriptiveImportException()
    {
        using var stream = CreateWorkbook(("Customers", ["CompanyName"]));

        var exception = Assert.Throws<InvalidImportFileException>(
            () => new ExcelWorkbookReader().Read(stream));

        Assert.Equal(
            "Column 'ExternalCustomerId' is missing in worksheet 'Customers'.",
            exception.Message);
    }

    private static MemoryStream CreateWorkbook(
        params (string Name, string[] Headers)[] sheets)
    {
        using var workbook = new XLWorkbook();
        foreach (var (name, headers) in sheets)
        {
            var worksheet = workbook.AddWorksheet(name);
            for (var index = 0; index < headers.Length; index++)
                worksheet.Cell(1, index + 1).Value = headers[index];
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
