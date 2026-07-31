using System.Text;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Validation;
using Enset.Infrastructure.Imports.Readers;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CsvImportTests
{
    [Fact]
    public void ReadRows_SemicolonAndGermanFormats_ReturnsMeterReadingRows()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value;Unit;QualityFlag\n" +
            "AT001;20.07.2026 14:30;12,75;kWh;1\n");

        var row = Assert.Single(new CsvMeterReadingReader().ReadRows(stream));
        var dto = MeterReadingExcelRowMapper.ToDto(row);

        Assert.Equal("AT001", dto.MeterNumber);
        Assert.Equal(12.75m, dto.Value);
        Assert.Equal("kWh", dto.Unit);
        Assert.Equal(1, dto.QualityFlag);
        Assert.False(dto.HasError, dto.ErrorMessage);
    }

    [Fact]
    public void ReadRows_CommaAndQuotedDecimal_ParsesQuotedField()
    {
        using var stream = Csv(
            "MeterNumber,Timestamp,Value,Unit\n" +
            "AT001,2026-07-20T14:30:00Z,\"12,75\",kWh\n");

        var row = Assert.Single(new CsvMeterReadingReader().ReadRows(stream));
        var dto = MeterReadingExcelRowMapper.ToDto(row);

        Assert.Equal(12.75m, dto.Value);
        Assert.False(dto.HasError, dto.ErrorMessage);
    }

    [Fact]
    public void ReadRows_InvalidValue_IsNotSkippedOrReplacedWithZero()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value\n" +
            "AT001;2026-07-20T14:30:00Z;invalid\n");

        var row = Assert.Single(new CsvMeterReadingReader().ReadRows(stream));
        var dto = MeterReadingExcelRowMapper.ToDto(row);

        Assert.Null(dto.Value);
        Assert.True(dto.HasError);
        Assert.Contains("Value 'invalid' is invalid", dto.ErrorMessage);
        Assert.Equal("invalid", dto.ValueRaw);
        Assert.Equal(2, dto.RowNumber);
    }

    [Fact]
    public void Mapper_PreservesEmptyRawValuesWithoutDefaultsOrErrors()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value;QualityFlag\n" +
            "AT001;;;\n");

        var dto = Assert.Single(new CsvMeterReadingReader().Read(stream));

        Assert.Null(dto.Timestamp);
        Assert.Null(dto.Value);
        Assert.Null(dto.QualityFlag);
        Assert.Equal(string.Empty, dto.TimestampRaw);
        Assert.Equal(string.Empty, dto.ValueRaw);
        Assert.Equal(string.Empty, dto.QualityRaw);
        Assert.False(dto.HasError, dto.ErrorMessage);
    }

    [Fact]
    public void Mapper_InvalidTimestampAndQualityPreserveRawValuesAndErrors()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value;QualityFlag\n" +
            "AT001;not-a-date;12,5;invalid-quality\n");

        var dto = Assert.Single(new CsvMeterReadingReader().Read(stream));

        Assert.Null(dto.Timestamp);
        Assert.Equal(12.5m, dto.Value);
        Assert.Null(dto.QualityFlag);
        Assert.Equal("not-a-date", dto.TimestampRaw);
        Assert.Equal("invalid-quality", dto.QualityRaw);
        Assert.True(dto.HasError);
        Assert.Contains("Timestamp", dto.ErrorMessage);
        Assert.Contains("QualityFlag", dto.ErrorMessage);
    }

    [Fact]
    public void ReadRows_WithoutMeterNumberUsesDefaultForEveryRow()
    {
        using var stream = Csv(
            "Timestamp;Value\n" +
            "2026-07-20;1.0\n" +
            "2026-07-21;\n");

        var rows = new CsvMeterReadingReader(" PROFILE-4711 ")
            .ReadRows(stream);
        var values = rows.Select(MeterReadingExcelRowMapper.ToDto).ToList();

        Assert.Equal(2, values.Count);
        Assert.All(values, value =>
            Assert.Equal("PROFILE-4711", value.MeterNumber));
        Assert.Null(values[1].Value);
        Assert.False(values[1].HasError, values[1].ErrorMessage);
    }

    [Fact]
    public void ReadRows_WithoutMeterNumberOrDefaultRemainsAnalyzable()
    {
        using var stream = Csv("Timestamp;Value\n2026-07-20;1.0\n");

        var dto = Assert.Single(
            new CsvMeterReadingReader().Read(stream));

        Assert.Equal(string.Empty, dto.MeterNumber);
        Assert.Null(dto.MeterId);
    }

    [Fact]
    public void ReadRows_MeterNumberColumnTakesPrecedenceOverDefault()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value\n" +
            "ROW-METER-1;2026-07-20;1.0\n" +
            "ROW-METER-2;2026-07-21;2.0\n");

        var rows = new CsvMeterReadingReader("WRONG-DEFAULT")
            .ReadRows(stream)
            .Select(MeterReadingExcelRowMapper.ToDto)
            .ToList();

        Assert.Equal("ROW-METER-1", rows[0].MeterNumber);
        Assert.Equal("ROW-METER-2", rows[1].MeterNumber);
        Assert.DoesNotContain(rows,
            reading => reading.MeterNumber == "WRONG-DEFAULT");
    }

    [Fact]
    public void Validate_CsvReadings_DoesNotRequireCustomerOrBuildingPayload()
    {
        using var stream = Csv(
            "MeterNumber;Timestamp;Value\n" +
            "AT001;2026-07-20T14:30:00Z;1.25\n");
        var readings = new CsvMeterReadingReader().ReadRows(stream);

        var report = new ExcelImportValidator().Validate(
            [],
            [],
            [],
            readings,
            ImportSourceType.Csv);

        Assert.Empty(report.Issues);
        Assert.Equal(1, report.MeterReadingCount);
        Assert.Equal(0, report.CustomerCount);
        Assert.Equal(0, report.BuildingCount);
    }

    private static MemoryStream Csv(string value) =>
        new(Encoding.UTF8.GetBytes(value));
}
