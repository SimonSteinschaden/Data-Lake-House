using System.Globalization;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class MeterReadingExcelRowMapper
{
    public static MeterReadingImportDto ToDto(MeterReadingExcelRow row)
    {
        var timestampValid = DateTime.TryParse(
            row.Timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
            out var timestamp);
        var valueValid = decimal.TryParse(
            row.Value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value);
        var qualityValid = string.IsNullOrWhiteSpace(row.QualityFlag) ||
            int.TryParse(row.QualityFlag, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

        var errors = new List<string>();
        if (!timestampValid)
            errors.Add($"Timestamp '{row.Timestamp}' is invalid");
        if (!valueValid)
            errors.Add($"Value '{row.Value}' is invalid");
        if (!qualityValid)
            errors.Add($"QualityFlag '{row.QualityFlag}' is invalid");

        return new MeterReadingImportDto
        {
            MeterNumber = row.MeterNumber?.Trim() ?? string.Empty,
            Timestamp = timestampValid ? timestamp : default,
            Value = valueValid ? value : null,
            Unit = row.Unit,
            QualityFlag = int.TryParse(
                row.QualityFlag,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var qualityFlag)
                ? qualityFlag
                : null,
            HasError = errors.Count > 0,
            ErrorMessage = errors.Count > 0 ? string.Join("; ", errors) : null
        };
    }
}
