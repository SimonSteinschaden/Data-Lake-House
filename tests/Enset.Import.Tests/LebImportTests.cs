using System.Text;
using System.Text.Json;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Leb;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Infrastructure.Imports.Leb;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Resolution;
using Xunit;

namespace Enset.Import.Tests;

public sealed class LebImportTests
{
    [Fact]
    public void CsvReader_IgnoresEmptyLinesAndRepeatedHeaders()
    {
        var path = WriteCsv(
            Header + "\n" +
            Data("1", "10", "20", "1", "2") + "\n\n" +
            Header + "\n" +
            Data("1", "11", "21", "3", "4"));

        try
        {
            var result = new LebWorkbookReader().Read(path);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("10", result.Rows[0].BuildingId);
            Assert.Equal("11", result.Rows[1].BuildingId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CsvReader_GeneratesStableHeadersAndPreservesPhysicalValues()
    {
        var path = WriteCsv(
            RealHeader + "\n" + RealData("Bezirk") + "\n" +
            RealHeader + "\n" + RealData("Bezirk 2"));
        try
        {
            var result = new LebWorkbookReader().Read(path);
            Assert.Equal(38, result.Columns.Count);
            Assert.Equal("Tabelle1", result.Columns[2].EffectiveHeader);
            Assert.Equal("Tabelle6", result.Columns[9].EffectiveHeader);
            Assert.Equal(3, result.Columns[2].Index);
            Assert.Equal("Bezirk", result.Rows[0].SourceValues["Tabelle1"]);
            Assert.Equal("Bezirk 2", result.Rows[1].SourceValues["Tabelle1"]);
            Assert.Equal(2, result.Columns[2].Values.Count);
            Assert.Equal("ReadingYear", result.Columns[12].EffectiveHeader);
            Assert.Equal("AnnualTotal", result.Columns[31].EffectiveHeader);
            Assert.Equal("865988", result.Rows[0].BuildingId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Mapper_CreatesSeparatedMunicipalIdentityAndMonthlyReadings()
    {
        var source = new LebWorkbookDto
        {
            Rows =
            [
                new LebRowDto
                {
                    RowNumber = 2,
                    MunicipalityId = "123",
                    MunicipalityName = "Testgemeinde",
                    BuildingId = "45",
                    BuildingName = "Rathaus",
                    Year = "2025",
                    MeterId = "99",
                    MeterName = "Hauptzähler",
                    Unit = "kWh",
                    AnnualValue = "78",
                    MonthlyValues = ["12,5", "13", null, null, null, null,
                        null, null, null, null, null, null]
                }
            ]
        };

        var workbook = new LebWorkbookMapper().Map(source, ImportMedium.Electricity);

        Assert.Equal(ImportSourceType.Landesenergiebuchhaltung, workbook.SourceType);
        Assert.Equal("LEB:GEM:123", Assert.Single(workbook.Customers).InternalCustomerId);
        Assert.Equal("LEB:GEM:123:GEB:45",
            Assert.Single(workbook.Buildings).InternalBuildingId);
        var meter = Assert.Single(workbook.Meters);
        Assert.Equal("99", meter.MeterNumber);
        Assert.Equal("Hauptzähler", meter.Name);
        Assert.Equal("Electricity", meter.ProfileName);
        Assert.Equal(78m, meter.AnnualValue);
        Assert.Equal(2025, meter.AnnualValueReferenceYear);
        Assert.Equal(2, workbook.MeterReadings.Count);
        Assert.StartsWith("2025-01-01", workbook.MeterReadings[0].Timestamp);
        Assert.StartsWith("2025-02-01", workbook.MeterReadings[1].Timestamp);
    }

    [Theory]
    [InlineData(ImportMedium.Electricity, "Electricity")]
    [InlineData(ImportMedium.Heat, "Heat")]
    public void Mapper_UsesUserSelectedMedium(ImportMedium medium, string expected)
    {
        var source = ValidSource();
        var workbook = new LebWorkbookMapper().Map(source, medium);
        Assert.Equal(expected, Assert.Single(workbook.Meters).ProfileName);
    }

    [Fact]
    public void Validator_ReportsMissingRequiredFieldsAndInvalidNumbers()
    {
        var source = new LebWorkbookDto
        {
            Rows =
            [
                new LebRowDto
                {
                    RowNumber = 7,
                    Year = "x",
                    MonthlyValues = ["invalid"],
                    AnnualValue = null
                }
            ]
        };

        var report = new LebImportValidator(source).Validate([], [], [], []);

        Assert.Contains(report.Issues, x => x.FieldName == "GemID");
        Assert.Contains(report.Issues, x => x.FieldName == "GebID");
        Assert.Contains(report.Issues, x => x.FieldName == "ZId");
        Assert.Contains(report.Issues, x => x.FieldName == "Zähler");
        Assert.Contains(report.Issues, x => x.FieldName == "AnnualTotal");
        Assert.Contains(
            report.Issues,
            x => x.Type == ImportIssueType.InvalidNumberFormat);
    }

    [Fact]
    public void CsvReader_RejectsMissingHeaders()
    {
        var path = WriteCsv("GemID;GebID;ZId\n1;2;3");
        try
        {
            Assert.Throws<InvalidImportFileException>(
                () => new LebWorkbookReader().Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xlsm")]
    [InlineData(".xls")]
    public void Reader_RejectsExcelFiles(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        Assert.Throws<InvalidImportFileException>(
            () => new LebWorkbookReader().Read(path));
    }

    [Fact]
    public void Validator_OnlyRequiresResolutionForGeneratedColumnsWithData()
    {
        var source = new LebWorkbookDto
        {
            Rows = ValidSource().Rows,
            Columns =
            [
                new LebSourceColumn
                {
                    Index = 3,
                    EffectiveHeader = "Tabelle1",
                    WasHeaderGenerated = true,
                    HasData = true
                },
                new LebSourceColumn
                {
                    Index = 4,
                    EffectiveHeader = "Tabelle2",
                    WasHeaderGenerated = true,
                    HasData = false
                }
            ]
        };

        var report = new LebImportValidator(source).Validate([], [], [], []);
        var issue = Assert.Single(report.Issues, x =>
            x.Type == ImportIssueType.SourceColumnMappingRequired);
        Assert.Equal("Tabelle1", issue.FieldName);
        Assert.DoesNotContain(report.Issues, x => x.FieldName == "Tabelle2");
    }

    [Fact]
    public void CustomColumnName_IsAppliedAndSurvivesReportSerialization()
    {
        var source = new LebWorkbookDto
        {
            Rows = ValidSource().Rows,
            Columns =
            [
                new LebSourceColumn
                {
                    Index = 3,
                    EffectiveHeader = "Tabelle1",
                    WasHeaderGenerated = true,
                    HasData = true
                }
            ]
        };
        var report = new LebImportValidator(source).Validate([], [], [], []);
        report.RecalculateCommitReadiness();
        var issue = Assert.Single(report.Issues, x =>
            x.Type == ImportIssueType.SourceColumnMappingRequired);

        new ApplyResolutionService().Apply(
            report,
            [
                new ImportIssueResolution
                {
                    IssueId = issue.IssueId,
                    ResolutionAction = ImportResolutionAction.UseCustomValue,
                    CustomResolvedValue = "Bezirk"
                }
            ],
            "test-user",
            DateTime.UtcNow);

        var restored = JsonSerializer.Deserialize<Enset.Application.Imports.Reports.ImportReport>(
            JsonSerializer.Serialize(report));
        Assert.Equal("Bezirk", Assert.Single(restored!.SourceColumns).EffectiveHeader);
        Assert.Equal(
            "Bezirk",
            Assert.Single(
                restored.Issues,
                x => x.Type == ImportIssueType.SourceColumnMappingRequired)
                .CustomResolvedValue);
    }

    [Theory]
    [InlineData("Auszug_EBN_Gebaeude_Hauptzähler_Strom_2025.csv")]
    [InlineData("Auszug_EBN_Gebaeude_Hauptzähler_Wärme_2025.csv")]
    public void RealLebCsv_CanBeReadWithoutLosingHeaderPositions(string fileName)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(), "Externe Daten", fileName);
        if (!File.Exists(path))
            return;

        var result = new LebWorkbookReader().Read(path);

        Assert.NotEmpty(result.Rows);
        Assert.Equal(38, result.Columns.Count);
        Assert.Equal(
            ["Tabelle1", "Tabelle2", "Tabelle3", "Tabelle4", "Tabelle5", "Tabelle6"],
            result.Columns.Where(x => x.WasHeaderGenerated)
                .Select(x => x.EffectiveHeader));
        Assert.Equal("ReadingYear", result.Columns[12].EffectiveHeader);
        Assert.Equal("AnnualTotal", result.Columns[31].EffectiveHeader);
    }

    private static LebWorkbookDto ValidSource() => new()
    {
        Rows =
        [
            new LebRowDto
            {
                RowNumber = 2,
                MunicipalityId = "1",
                BuildingId = "2",
                Year = "2025",
                MeterId = "3",
                MeterName = "Zähler",
                AnnualValue = "10",
                MonthlyValues = ["10"]
            }
        ]
    };

    private const string Header =
        "GemID;Gemeinde;GebID;Gebäude;Baujahr;m2;Jahr;ZId;Zähler;Typ;Einheit;" +
        "Medium;MGruppe;Jan;Feb;Mrz;Apr;Mai;Jun;Jul;Aug;Sep;Okt;Nov;Dez;Jahr";

    private const string RealHeader =
        "GemID;Gemeinde;;;;;GebID;Gebäude;;;Baujahr;m2;Jahr;ZId;Zähler;Typ;" +
        "Einheit;Medium;MGruppe;Jan;Feb;Mrz;Apr;Mai;Jun;Jul;Aug;Sep;Okt;Nov;" +
        "Dez;Jahr;MIND;MINW;MAXD;MAXW;MIND1;MINW1";

    private static string Data(
        string municipality, string building, string meter, string jan, string annual) =>
        $"{municipality};Gemeinde;{building};Rathaus;1990;100;2025;{meter};Zähler;" +
        $"H;kWh;ignored;Strom;{jan};;;;;;;;;;;;{annual}";

    private static string RealData(string unnamedColumnValue)
    {
        var values = new string[38];
        values[0] = "579465";
        values[1] = "Absdorf";
        values[2] = unnamedColumnValue;
        values[6] = "865988";
        values[7] = "Alte Post";
        values[10] = "1968";
        values[11] = "342";
        values[12] = "2025";
        values[13] = "924117";
        values[14] = "Strom Hauptzähler";
        values[15] = "H";
        values[16] = "kWh";
        values[17] = "Electricity3";
        values[18] = "Strom";
        values[19] = "268,22";
        values[31] = "2.465,17";
        return string.Join(';', values);
    }

    private static string WriteCsv(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, contents, new UTF8Encoding(false));
        return path;
    }
}
