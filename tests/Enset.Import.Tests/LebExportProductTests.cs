using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Enset.Api.Controllers;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Mapping;
using Enset.Application.Exports.LEB.Models;
using Enset.Application.Exports.LEB.Services;
using Enset.Application.Exports.LEB.Validation;
using Enset.Application.CanonicalSnapshots;
using Enset.Infrastructure.Exports.LEB;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Enset.Import.Tests;

public sealed class LebExportProductTests
{
    [Fact]
    public void Mappings_are_central_and_return_no_dummy_for_unknown_values()
    {
        var electricity = NoeEnergyCarrierMapper.Map("Electricity");

        Assert.Equal("Strom", electricity.Carrier);
        Assert.Equal("STROM", electricity.Medium);
        Assert.Equal("BEZUG", NoeMeasurementDirectionMapper.Map("Consumption"));
        Assert.Equal("INTERVALLWERT", NoeReadingTypeMapper.Map("IntervalValue"));
        Assert.Equal("HAUPTZAEHLER", NoeMeterCategoryMapper.Map("Physical"));
        Assert.Null(NoeEnergyCarrierMapper.Map("Unmapped").Medium);
        Assert.Null(NoeBuildingUsageMapper.Map("Unmapped"));
    }

    [Fact]
    public void Validator_blocks_required_fields_and_keeps_warnings_non_blocking()
    {
        var invalid = Contract(
            objects: [Object() with
            {
                ObjectCode = null, UsageType = null, ConditionedGrossFloorArea = null
            }],
            meters: [Meter() with
            {
                ObjectId = null, EnergyCarrier = null, NoeNavigatorMedium = null,
                MeterCategory = null, Unit = null
            }],
            readings: [Reading() with { ReadingTimestamp = null, ReadingValue = null }]);

        var result = new LebExportValidator().Validate(invalid);

        Assert.False(result.CanExport);
        Assert.Contains(result.Errors, x => x.Code == "OBJECT_CODE");
        Assert.Contains(result.Errors, x => x.Code == "CONDITIONED_AREA");
        Assert.Contains(result.Errors, x => x.Code == "NAVIGATOR_MEDIUM");
        Assert.Contains(result.Errors, x => x.Code == "READING_TIMESTAMP");
        Assert.Contains(result.Warnings, x => x.Code == "CONSTRUCTION_YEAR");
    }

    [Fact]
    public void Validator_allows_empty_contract_and_warning_only_contract()
    {
        var empty = new LebExportValidator().Validate(Contract());
        var warnings = new LebExportValidator().Validate(Contract(
            objects: [Object()], meters: [Meter()], readings: [Reading()]));

        Assert.True(empty.CanExport);
        Assert.True(warnings.CanExport);
        Assert.NotEmpty(warnings.Warnings);
    }

    [Fact]
    public void Leb_suitability_is_independent_from_quality_level()
    {
        var bronzeSuitable = new LebExportDataset(
            Contract(),
            [new("Meter", Guid.NewGuid(), "AT001", "Bronze",
                SuitabilityStatus.Suitable)],
            DateTime.UtcNow);
        var goldNotSuitable = new LebExportDataset(
            Contract(),
            [new("Meter", Guid.NewGuid(), "AT002", "Gold",
                SuitabilityStatus.NotSuitable)],
            DateTime.UtcNow);

        var suitable = new LebExportValidator().Validate(bronzeSuitable);
        var blocked = new LebExportValidator().Validate(goldNotSuitable);

        Assert.True(suitable.CanExport);
        Assert.Equal(1, suitable.SuitableCount);
        Assert.False(blocked.CanExport);
        Assert.Equal(1, blocked.NotSuitableCount);
        Assert.Contains(blocked.Errors,
            x => x.Code == "LEB_NOT_SUITABLE");
    }

    [Fact]
    public void Csv_export_creates_five_utf8_semicolon_files_with_invariant_decimals()
    {
        var contract = Contract(
            municipalities:
            [
                Municipality(Guid.NewGuid(), "101", "Gemeinde A"),
                Municipality(Guid.NewGuid(), "102", "Gemeinde B")
            ],
            objects: [Object(), Object() with { ObjectId = Guid.NewGuid(), ObjectCode = "B-2" }],
            meters: [Meter(), Meter() with
                { MeterId = Guid.NewGuid(), EnergyCarrier = "Erdgas", Unit = "m³" }],
            readings: [Reading() with { ReadingValue = 12.5m }]);

        var file = new CsvLebExporter().Export(contract);
        using var zip = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);

        Assert.Equal(5, zip.Entries.Count);
        Assert.Contains(zip.Entries, x => x.Name == "Municipalities.csv");
        var readings = Read(zip, "Readings.csv");
        Assert.Contains(';', readings);
        Assert.Contains("12.5", readings);
        Assert.StartsWith("\uFEFF", readings);
    }

    [Fact]
    public void Excel_export_creates_five_clean_worksheets()
    {
        var file = new ExcelLebExporter().Export(Contract(
            municipalities: [Municipality(Guid.NewGuid(), "101", "Gemeinde")],
            objects: [Object()], meters: [Meter()], readings: [Reading()],
            systems: [new(Guid.NewGuid(), ObjectId, "Photovoltaic", "Strom",
                25m, null, null, null)]));

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));

        Assert.Equal(5, workbook.Worksheets.Count);
        Assert.Equal("Municipalities", workbook.Worksheet(1).Name);
        Assert.Equal("Objects", workbook.Worksheet(2).Name);
        Assert.Equal("Meters", workbook.Worksheet(3).Name);
        Assert.Equal("Readings", workbook.Worksheet(4).Name);
        Assert.Equal("EnergySystems", workbook.Worksheet(5).Name);
        Assert.Equal("ObjectId", workbook.Worksheet("Objects").Cell(1, 1).GetString());
    }

    [Fact]
    public async Task Service_validates_before_every_file_export()
    {
        var contract = Contract(objects: [Object() with { ObjectCode = null }]);
        var csv = new RecordingCsvExporter();
        var excel = new RecordingExcelExporter();
        var service = new LebExportService(new StubBuilder(contract),
            new LebExportValidator(), csv, excel);

        await Assert.ThrowsAsync<LebExportValidationException>(() =>
            service.ExportCsvAsync(new(), default));
        await Assert.ThrowsAsync<LebExportValidationException>(() =>
            service.ExportExcelAsync(new(), default));
        Assert.False(csv.Called);
        Assert.False(excel.Called);
    }

    [Fact]
    public async Task Api_returns_validation_and_files_and_422_for_invalid_contract()
    {
        var valid = new StubExportService(Contract());
        var controller = new LebExportsController(valid);

        var validation = await controller.Validate(new(), default);
        Assert.IsType<OkObjectResult>(validation);
        Assert.IsType<FileContentResult>(await controller.Csv(new(), default));
        Assert.IsType<FileContentResult>(await controller.Excel(new(), default));

        valid.FailExport = true;
        Assert.IsType<UnprocessableEntityObjectResult>(
            await controller.Csv(new(), default));
    }

    private static readonly Guid ObjectId = Guid.NewGuid();
    private static NoeLebExportContractV1 Contract(
        IReadOnlyList<LebMunicipalityRow>? municipalities = null,
        IReadOnlyList<LebObjectRow>? objects = null,
        IReadOnlyList<LebMeterRow>? meters = null,
        IReadOnlyList<LebReadingRow>? readings = null,
        IReadOnlyList<LebEnergySystemRow>? systems = null) =>
        new(NoeLebExportContractV1.Name, NoeLebExportContractV1.Version,
            new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc),
            municipalities ?? [], objects ?? [], meters ?? [], readings ?? [], systems ?? []);

    private static LebMunicipalityRow Municipality(Guid id, string number, string name) =>
        new(id, number, name, "Hauptregion", DateTime.UtcNow);
    private static LebObjectRow Object() => new(ObjectId, Guid.NewGuid(), "Public",
        "OBJ-1", "Rathaus", "BUERO", "Office", "Hauptplatz 1", "3100", "Stadt",
        null, null, null, 100m, null, null, null, null, null, null, null, null, null);
    private static LebMeterRow Meter() => new(Guid.NewGuid(), ObjectId, "Hauptzähler",
        "M-1", null, "Physical", "HAUPTZAEHLER", "Strom", "STROM",
        "ELEKTRISCH", "BEZUG", "INTERVALLWERT", "KWh", null, null);
    private static LebReadingRow Reading() => new(Guid.NewGuid(), DateTime.UtcNow,
        1m, "KWh", "INTERVALLWERT", "Measured", "Imported", false);

    private static string Read(ZipArchive zip, string name)
    {
        using var reader = new StreamReader(zip.GetEntry(name)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class StubBuilder(NoeLebExportContractV1 contract) : INoeLebContractBuilder
    {
        public Task<LebExportDataset> BuildAsync(
            LebExportRequest request, CancellationToken ct) =>
            Task.FromResult(new LebExportDataset(
                contract,
                [],
                contract.ExportTimestamp));
    }
    private sealed class RecordingCsvExporter : ICsvLebExporter
    {
        public bool Called { get; private set; }
        public LebExportFile Export(NoeLebExportContractV1 contract)
        { Called = true; return new([], "application/zip", "export.zip"); }
    }
    private sealed class RecordingExcelExporter : IExcelLebExporter
    {
        public bool Called { get; private set; }
        public LebExportFile Export(NoeLebExportContractV1 contract)
        { Called = true; return new([], "application/xlsx", "export.xlsx"); }
    }
    private sealed class StubExportService(NoeLebExportContractV1 contract) : ILebExportService
    {
        public bool FailExport { get; set; }
        public Task<ValidationResult> ValidateAsync(LebExportRequest request, CancellationToken ct) =>
            Task.FromResult(new LebExportValidator().Validate(contract));
        public Task<LebExportFile> ExportCsvAsync(LebExportRequest request, CancellationToken ct) =>
            Export("application/zip", "export.zip");
        public Task<LebExportFile> ExportExcelAsync(LebExportRequest request, CancellationToken ct) =>
            Export("application/xlsx", "export.xlsx");
        private Task<LebExportFile> Export(string type, string name)
        {
            if (FailExport) throw new LebExportValidationException(
                new(false, [new("ERROR", "Objects", null, "Field", "Invalid")], []));
            return Task.FromResult(new LebExportFile([1], type, name));
        }
    }
}
