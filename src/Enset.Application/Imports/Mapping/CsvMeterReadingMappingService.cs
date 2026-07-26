using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class CsvMeterReadingMappingService
{
    public static IReadOnlyList<MeterReadingExcelRow> Map(
        CsvMeterReadingMapping mapping,
        string? defaultMeterNumber,
        Guid? assignedMeterId = null)
    {
        return mapping.RawRows.Select((raw, index) =>
        {
            var timestamp = mapping.TimestampSource == ImportFieldSource.Generated
                ? mapping.StartTimestamp!.Value
                    .AddTicks(mapping.SamplingInterval!.Value.Ticks * index)
                    .ToString("O")
                : Get(raw, mapping.TimestampColumn);
            return new MeterReadingExcelRow
            {
                RowNumber = raw.RowNumber,
                MeterNumber = Get(raw, mapping.MeterNumberColumn),
                MeterId = assignedMeterId,
                DefaultMeterNumber = mapping.MeterNumberColumn is null
                    ? defaultMeterNumber
                    : null,
                Timestamp = timestamp,
                TimestampSource = mapping.TimestampSource,
                Value = Get(raw, mapping.ValueColumn),
                ValueSource = mapping.ValueSource,
                Unit = Get(raw, mapping.UnitColumn),
                QualityFlag = Get(raw, mapping.QualityColumn),
                QualitySource = mapping.QualityColumn is null
                    ? ImportFieldSource.Generated
                    : mapping.QualitySource,
                ParsingError = raw.ParsingError
            };
        }).ToList();
    }

    private static string? Get(CsvRawRow row, string? column) =>
        column is not null && row.Values.TryGetValue(column, out var value)
            ? value
            : null;
}
