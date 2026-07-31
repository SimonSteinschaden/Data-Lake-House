using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Exceptions;
using Enset.Infrastructure.Imports.Analysis;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportFileExtensionValidationTests
{
    [Theory]
    [InlineData("leb.csv")]
    [InlineData("LEB.CSV")]
    public void Landesenergiebuchhaltung_AcceptsCsv(string fileName)
    {
        ExcelImportAnalysisService.ValidateFileExtension(
            fileName,
            ImportSourceType.Landesenergiebuchhaltung);
    }

    [Theory]
    [InlineData("leb.xlsx")]
    [InlineData("leb.xlsm")]
    [InlineData("leb.xls")]
    public void Landesenergiebuchhaltung_RejectsExcelWithCsvMessage(
        string fileName)
    {
        var exception = Assert.Throws<InvalidImportFileException>(() =>
            ExcelImportAnalysisService.ValidateFileExtension(
                fileName,
                ImportSourceType.Landesenergiebuchhaltung));

        Assert.Equal(
            "Die Landesenergiebuchhaltung erwartet eine CSV-Datei (*.csv).",
            exception.Message);
    }

    [Fact]
    public void LoadProfile_AcceptsCsv()
    {
        ExcelImportAnalysisService.ValidateFileExtension(
            "lastprofil.csv",
            ImportSourceType.Csv);
    }

    [Fact]
    public void LoadProfile_RejectsExcelWithCsvMessage()
    {
        var exception = Assert.Throws<InvalidImportFileException>(() =>
            ExcelImportAnalysisService.ValidateFileExtension(
                "lastprofil.xlsx",
                ImportSourceType.Csv));

        Assert.Equal(
            "Das Lastprofil erwartet eine CSV-Datei (*.csv).",
            exception.Message);
    }

    [Theory]
    [InlineData("crm.xlsx")]
    [InlineData("crm.xlsm")]
    public void CrmExcel_KeepsExistingExcelRules(string fileName)
    {
        ExcelImportAnalysisService.ValidateFileExtension(
            fileName,
            ImportSourceType.CRM_Excel);
    }

    [Fact]
    public void CrmExcel_StillRejectsCsv()
    {
        Assert.Throws<InvalidImportFileException>(() =>
            ExcelImportAnalysisService.ValidateFileExtension(
                "crm.csv",
                ImportSourceType.CRM_Excel));
    }
}
