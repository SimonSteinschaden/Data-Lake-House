using System.Globalization;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class MeterReadingExcelRowMapper
{
    public static MeterReadingImportDto ToDto(MeterReadingExcelRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        DateTime timestamp = default;
        decimal value = default;
        int qualityFlag = default;

        var timestampMissing = string.IsNullOrWhiteSpace(row.Timestamp);
        var timestampValid = timestampMissing ||
                             TryParseTimestamp(row.Timestamp, out timestamp);

        var valueMissing = string.IsNullOrWhiteSpace(row.Value);
        var valueValid = valueMissing ||
                         TryParseDecimal(row.Value, out value);

        var qualityMissing = string.IsNullOrWhiteSpace(row.QualityFlag);
        var qualityValid = qualityMissing ||
                           int.TryParse(
                               row.QualityFlag,
                               NumberStyles.Integer,
                               CultureInfo.InvariantCulture,
                               out qualityFlag);

        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(row.ParsingError))
        {
            errors.Add(row.ParsingError);
        }

        if (!timestampMissing && !timestampValid)
        {
            errors.Add($"Timestamp '{row.Timestamp}' is invalid.");
        }

        if (!valueMissing && !valueValid)
        {
            errors.Add($"Value '{row.Value}' is invalid.");
        }

        if (!qualityMissing && !qualityValid)
        {
            errors.Add($"QualityFlag '{row.QualityFlag}' is invalid.");
        }

        return new MeterReadingImportDto
        {
            MeterNumber =
                (row.MeterNumber ?? row.DefaultMeterNumber)?.Trim() ??
                string.Empty,
            MeterId = row.MeterId,
            RowNumber = row.RowNumber,
            MeterNumberRaw = row.MeterNumber,
            TimestampRaw = row.TimestampSource == ImportFieldSource.Generated
                ? null
                : row.Timestamp,
            TimestampSource = row.TimestampSource,
            ValueRaw = row.Value,
            ValueSource = row.ValueSource,
            QualityRaw = row.QualityFlag,
            QualitySource = row.QualitySource,
            ParsingError = row.ParsingError,

            Timestamp = timestampMissing || !timestampValid
                ? null
                : timestamp,

            Value = valueMissing || !valueValid
                ? null
                : value,

            Unit = NormalizeOptionalValue(row.Unit),

            QualityFlag = qualityMissing || !qualityValid
                ? null
                : qualityFlag,

            HasError = errors.Count > 0,

            ErrorMessage = errors.Count == 0
                ? null
                : string.Join("; ", errors),

            ReadingType = row.ReadingType,
            IntervalSeconds = row.IntervalSeconds
        };
    }

    private static bool TryParseTimestamp(
        string? rawValue,
        out DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            timestamp = default;
            return false;
        }

        var value = rawValue.Trim();

        var styles =
            DateTimeStyles.AllowWhiteSpaces |
            DateTimeStyles.RoundtripKind;

        foreach (var culture in SupportedCultures)
        {
            if (DateTime.TryParse(value, culture, styles, out timestamp))
            {
                return true;
            }
        }

        timestamp = default;
        return false;
    }

    private static bool TryParseDecimal(
        string? rawValue,
        out decimal number)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            number = default;
            return false;
        }

        var value = rawValue.Trim();

        var cultures =
            value.LastIndexOf(',') > value.LastIndexOf('.')
                ? GermanCultures
                : SupportedCultures;

        foreach (var culture in cultures)
        {
            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    culture,
                    out number))
            {
                return true;
            }
        }

        number = default;
        return false;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static readonly CultureInfo[] SupportedCultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("de-AT"),
        CultureInfo.GetCultureInfo("de-DE")
    ];

    private static readonly CultureInfo[] GermanCultures =
    [
        CultureInfo.GetCultureInfo("de-AT"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.InvariantCulture
    ];
}
