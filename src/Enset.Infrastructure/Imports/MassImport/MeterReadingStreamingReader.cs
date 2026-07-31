using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Enset.Infrastructure.Imports.MassImport;

public sealed record MeterReadingStagingRow(
    long SourceRowNumber,
    Guid? MeterId,
    string? MeterNumber,
    DateTime? Timestamp,
    decimal? Value,
    string? Unit,
    string QualityFlag,
    string ReadingType,
    string? EnergyDirection,
    int? IntervalSeconds,
    string? ValidationCode,
    string? ValidationMessage,
    string RawSourceHash);

public interface IMeterReadingStreamingReader
{
    IAsyncEnumerable<IReadOnlyList<MeterReadingStagingRow>> ReadChunks(
        Stream stream,
        Guid? assignedMeterId,
        string? defaultMeterNumber,
        int chunkSize,
        CancellationToken cancellationToken);
}

public sealed class MeterReadingStreamingReader
    : IMeterReadingStreamingReader
{
    public async IAsyncEnumerable<IReadOnlyList<MeterReadingStagingRow>>
        ReadChunks(
            Stream stream,
            Guid? assignedMeterId,
            string? defaultMeterNumber,
            int chunkSize,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            true,
            81920,
            leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidDataException("CSV header is missing.");
        var delimiter = DetectDelimiter(headerLine);
        var headers = Parse(headerLine, delimiter)
            .Select((value, index) => (
                Name: Normalize(value),
                Index: index))
            .ToDictionary(x => x.Name, x => x.Index);
        var timestampIndex = Find(
            headers,
            "timestamp", "datetime", "datum", "messzeitpunkt",
            "zeitstempel", "zeit");
        var valueIndex = Find(
            headers,
            "value", "wert", "messwert", "verbrauch",
            "energie", "leistung");
        var meterIndex = Find(
            headers,
            "meternumber", "meter", "zaehlernummer", "zaehler");
        var unitIndex = Find(headers, "unit", "einheit", "masseinheit");
        var qualityIndex = Find(
            headers,
            "quality", "qualityflag", "qualitaet", "status");
        var readingTypeIndex = Find(
            headers,
            "readingtype", "messwertart");
        var directionIndex = Find(
            headers,
            "energydirection", "richtung");
        var intervalIndex = Find(
            headers,
            "intervalseconds", "intervallsekunden");
        var chunk = new List<MeterReadingStagingRow>(chunkSize);
        long rowNumber = 1;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var values = Parse(line, delimiter);
            string? Get(int? index) =>
                index.HasValue && index.Value < values.Count
                    ? Clean(values[index.Value])
                    : null;
            var timestampRaw = Get(timestampIndex);
            var valueRaw = Get(valueIndex);
            var timestamp = ParseTimestamp(timestampRaw);
            var value = ParseDecimal(valueRaw);
            var code = timestampIndex is null
                ? "TIMESTAMP_COLUMN_MISSING"
                : valueIndex is null
                    ? "VALUE_COLUMN_MISSING"
                    : timestamp is null
                        ? "TIMESTAMP_INVALID"
                        : value is null
                            ? "VALUE_INVALID"
                            : null;
            chunk.Add(new(
                rowNumber,
                assignedMeterId,
                Get(meterIndex) ?? Clean(defaultMeterNumber),
                timestamp,
                value,
                Get(unitIndex),
                Get(qualityIndex) ?? "Measured",
                Get(readingTypeIndex) ?? "IntervalValue",
                Get(directionIndex),
                ParseInteger(Get(intervalIndex)),
                code,
                code is null ? null : $"Invalid CSV row {rowNumber}.",
                Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(line)))));
            if (chunk.Count < chunkSize)
                continue;
            yield return chunk;
            chunk = new(chunkSize);
        }
        if (chunk.Count > 0)
            yield return chunk;
    }

    private static int? Find(
        IReadOnlyDictionary<string, int> headers,
        params string[] names)
    {
        foreach (var name in names)
            if (headers.TryGetValue(name, out var index))
                return index;
        return null;
    }

    private static char DetectDelimiter(string value) =>
        new[] { ';', ',', '\t' }
            .OrderByDescending(x => value.Count(c => c == x))
            .First();

    private static IReadOnlyList<string> Parse(
        string value,
        char delimiter)
    {
        var result = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == '"')
            {
                if (quoted && i + 1 < value.Length &&
                    value[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                    quoted = !quoted;
            }
            else if (current == delimiter && !quoted)
            {
                result.Add(field.ToString());
                field.Clear();
            }
            else
                field.Append(current);
        }
        result.Add(field.ToString());
        return result;
    }

    private static DateTime? ParseTimestamp(string? value)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var invariant))
            return invariant.UtcDateTime;
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.GetCultureInfo("de-AT"),
                DateTimeStyles.AssumeLocal |
                DateTimeStyles.AdjustToUniversal,
                out var german))
            return german.UtcDateTime;
        return null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var invariant)
            ? invariant
            : decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("de-AT"),
                out var german)
                ? german
                : null;

    private static int? ParseInteger(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return new string(decomposed
                .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) !=
                            UnicodeCategory.NonSpacingMark)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray())
            .Replace("ß", "ss");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
