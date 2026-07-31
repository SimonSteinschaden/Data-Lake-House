using System.Text;
using System.Globalization;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Readers;

public sealed class CsvMeterReadingReader : IMeterReadingReader
{
    private static readonly string[] RequiredHeaders =
    [
        "Timestamp",
        "Value"
    ];
    private readonly string? _defaultMeterNumber;

    public CsvMeterReadingReader(string? defaultMeterNumber = null)
    {
        _defaultMeterNumber = string.IsNullOrWhiteSpace(defaultMeterNumber)
            ? null
            : defaultMeterNumber.Trim();
    }

    public string? DefaultMeterNumber => _defaultMeterNumber;

    public IEnumerable<MeterReadingImportDto> Read(Stream stream) =>
        ReadRows(stream).Select(MeterReadingExcelRowMapper.ToDto);

    public IReadOnlyList<MeterReadingExcelRow> ReadRows(Stream stream) =>
        CsvMeterReadingMappingService.Map(
            ReadMapping(stream),
            _defaultMeterNumber);

    public CsvMeterReadingMapping ReadMapping(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
            throw new InvalidImportFileException("The uploaded CSV stream is not readable.");

        if (stream.CanSeek)
            stream.Position = 0;

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidImportFileException("The CSV file is empty or has no header row.");

        var delimiter = DetectDelimiter(headerLine);
        var headerValues = ParseRecord(headerLine, delimiter, 1, out var headerError);
        if (headerError is not null)
            throw new InvalidImportFileException($"CSV header is invalid: {headerError}");

        var normalizedHeaders = headerValues
            .Select((name, index) => new { Name = name.Trim(), Index = index })
            .ToList();
        if (normalizedHeaders.Any(header => string.IsNullOrWhiteSpace(header.Name)))
            throw new InvalidImportFileException("The CSV header contains an empty column name.");

        var duplicateHeader = normalizedHeaders
            .GroupBy(header => header.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateHeader is not null)
        {
            throw new InvalidImportFileException(
                $"Column '{duplicateHeader}' occurs more than once in the CSV header.");
        }

        var headers = normalizedHeaders
            .ToDictionary(
                header => header.Name,
                header => header.Index,
                StringComparer.OrdinalIgnoreCase);

        var timestampCandidates = Detect(headerValues, TimestampNames);
        var valueCandidates = Detect(headerValues, ValueNames);
        var qualityCandidates = Detect(headerValues, QualityNames);
        var meterCandidates = Detect(headerValues, MeterNames);
        var unitCandidates = Detect(headerValues, UnitNames);
        var rows = new List<CsvRawRow>();
        var physicalLine = 1;
        while (!reader.EndOfStream)
        {
            var startLine = physicalLine + 1;
            var record = ReadLogicalRecord(reader, ref physicalLine);
            if (string.IsNullOrWhiteSpace(record))
                continue;

            var values = ParseRecord(record, delimiter, startLine, out var parsingError);
            if (values.Count != headerValues.Count)
            {
                parsingError = JoinErrors(
                    parsingError,
                    $"Expected {headerValues.Count} column(s), found {values.Count}");
            }

            rows.Add(new CsvRawRow
            {
                RowNumber = startLine,
                Values = headerValues
                    .Select((header, index) => new
                    {
                        Header = header.Trim(),
                        Value = index < values.Count ? values[index] : null
                    })
                    .ToDictionary(x => x.Header, x => x.Value,
                        StringComparer.OrdinalIgnoreCase),
                ParsingError = parsingError
            });
        }

        if (rows.Count == 0)
            throw new InvalidImportFileException("The CSV file contains no data rows.");

        return new CsvMeterReadingMapping
        {
            DetectedHeaders = headerValues.Select(x => x.Trim()).ToList(),
            RawRows = rows,
            TimestampColumn = Single(timestampCandidates),
            TimestampSource = timestampCandidates.Count == 1
                ? ImportFieldSource.FileColumn
                : ImportFieldSource.Missing,
            ValueColumn = Single(valueCandidates),
            ValueSource = valueCandidates.Count == 1
                ? ImportFieldSource.FileColumn
                : ImportFieldSource.Missing,
            QualityColumn = Single(qualityCandidates),
            QualitySource = qualityCandidates.Count == 1
                ? ImportFieldSource.FileColumn
                : ImportFieldSource.Generated,
            MeterNumberColumn = Single(meterCandidates),
            UnitColumn = Single(unitCandidates)
        };
    }

    private static string? Single(IReadOnlyList<string> values) =>
        values.Count == 1 ? values[0] : null;

    private static IReadOnlyList<string> Detect(
        IReadOnlyList<string> headers,
        IReadOnlySet<string> synonyms) =>
        headers.Where(header =>
        {
            var normalized = NormalizeHeader(header);
            return synonyms.Any(name =>
                normalized == name ||
                normalized.StartsWith(name, StringComparison.Ordinal));
        }).ToList();

    private static string NormalizeHeader(string header)
    {
        var withoutUnit = System.Text.RegularExpressions.Regex.Replace(
            header.Trim(), @"\([^)]*\)", string.Empty);
        var decomposed = withoutUnit.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString().Replace("ß", "ss");
    }

    private static readonly IReadOnlySet<string> TimestampNames =
        Names("Timestamp", "DateTime", "Datum", "Uhrzeit", "Datum/Uhrzeit",
            "Messzeitpunkt", "Zeit", "Zeitstempel");
    private static readonly IReadOnlySet<string> ValueNames =
        Names("Value", "Wert", "Messwert", "Verbrauch", "Energie",
            "Leistung", "Bezug", "Einspeisung");
    private static readonly IReadOnlySet<string> QualityNames =
        Names("Quality", "QualityFlag", "Qualität", "Status", "Validity");
    private static readonly IReadOnlySet<string> MeterNames =
        Names("MeterNumber", "Meter", "Zählernummer", "Zaehlernummer",
            "Zähler", "MeterId");
    private static readonly IReadOnlySet<string> UnitNames =
        Names("Unit", "Einheit", "Maßeinheit", "Masseinheit");

    private static IReadOnlySet<string> Names(params string[] names) =>
        names.Select(NormalizeHeader).ToHashSet(StringComparer.Ordinal);

    private static char DetectDelimiter(string header)
    {
        var candidates = new[] { ';', ',', '\t' }
            .Select(delimiter => new
            {
                Delimiter = delimiter,
                Count = CountOutsideQuotes(header, delimiter)
            })
            .OrderByDescending(candidate => candidate.Count)
            .ToList();

        return candidates[0].Count > 0
            ? candidates[0].Delimiter
            : throw new InvalidImportFileException(
                "The CSV delimiter could not be detected. Supported delimiters are semicolon, comma and tab.");
    }

    private static int CountOutsideQuotes(string value, char delimiter)
    {
        var count = 0;
        var quoted = false;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '"')
            {
                if (quoted && index + 1 < value.Length && value[index + 1] == '"')
                    index++;
                else
                    quoted = !quoted;
            }
            else if (!quoted && value[index] == delimiter)
            {
                count++;
            }
        }

        return count;
    }

    private static string ReadLogicalRecord(
        StreamReader reader,
        ref int physicalLine)
    {
        var record = new StringBuilder();
        var quoted = false;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine() ?? string.Empty;
            physicalLine++;
            if (record.Length > 0)
                record.AppendLine();
            record.Append(line);
            quoted = HasOpenQuote(record.ToString());
            if (!quoted)
                break;
        }

        return record.ToString();
    }

    private static bool HasOpenQuote(string value)
    {
        var quoted = false;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '"')
                continue;

            if (quoted && index + 1 < value.Length && value[index + 1] == '"')
                index++;
            else
                quoted = !quoted;
        }

        return quoted;
    }

    private static List<string> ParseRecord(
        string record,
        char delimiter,
        int rowNumber,
        out string? parsingError)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        parsingError = null;

        for (var index = 0; index < record.Length; index++)
        {
            var character = record[index];
            if (character == '"')
            {
                if (quoted && index + 1 < record.Length && record[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == delimiter && !quoted)
            {
                values.Add(value.ToString().Trim());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString().Trim());
        if (quoted)
            parsingError = $"CSV row {rowNumber} contains an unterminated quoted field";

        return values;
    }

    private static string? Get(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        return headers.TryGetValue(header, out var index) && index < values.Count
            ? values[index]
            : null;
    }

    private static string JoinErrors(string? first, string second) =>
        string.IsNullOrWhiteSpace(first) ? second : $"{first}; {second}";
}
