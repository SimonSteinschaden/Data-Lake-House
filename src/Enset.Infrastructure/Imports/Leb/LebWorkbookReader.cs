using System.Text;
using ClosedXML.Excel;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Leb.DTOs;

namespace Enset.Infrastructure.Imports.Leb;

public sealed class LebWorkbookReader
{
    private static readonly string[] RequiredHeaders =
        ["GemID", "GebID", "ReadingYear", "ZId", "Zähler", "Jan", "Feb", "Mrz",
         "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
         "AnnualTotal"];

    public LebWorkbookDto Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".xlsx" or ".xlsm" => ReadXlsx(filePath),
            ".csv" => ReadCsv(filePath),
            var extension => throw new InvalidImportFileException(
                $"Dateityp '{extension}' wird für die Landesenergiebuchhaltung nicht unterstützt.")
        };
    }

    private static LebWorkbookDto ReadXlsx(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var rows = new List<LebRowDto>();
            IReadOnlyList<LebSourceColumn>? columns = null;
            foreach (var worksheet in workbook.Worksheets)
            {
                var used = worksheet.RangeUsed();
                if (used is null)
                    continue;

                HeaderMap? headers = null;
                foreach (var row in used.Rows())
                {
                    var values = row.Cells(1, used.ColumnCount())
                        .Select(cell => cell.GetFormattedString().Trim())
                        .ToArray();
                    ProcessRow(
                        row.RowNumber(), values, ref headers, rows, ref columns);
                }
            }

            EnsureData(rows);
            return CreateWorkbook(columns, rows);
        }
        catch (InvalidImportFileException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidImportFileException(
                "Die LEB-Datei ist keine gültige Excel-Arbeitsmappe.", exception);
        }
    }

    private static LebWorkbookDto ReadCsv(string filePath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
            throw new InvalidImportFileException("Die LEB-CSV-Datei ist leer.");

        var text = Decode(bytes);
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var firstContentLine = lines.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? throw new InvalidImportFileException("Die LEB-CSV-Datei ist leer.");
        var delimiter = DetectDelimiter(firstContentLine);
        var rows = new List<LebRowDto>();
        HeaderMap? headers = null;
        IReadOnlyList<LebSourceColumn>? columns = null;

        for (var index = 0; index < lines.Length; index++)
        {
            ProcessRow(
                index + 1,
                ParseCsvLine(lines[index], delimiter),
                ref headers,
                rows,
                ref columns);
        }

        EnsureData(rows);
        return CreateWorkbook(columns, rows);
    }

    private static void ProcessRow(
        int rowNumber,
        IReadOnlyList<string> values,
        ref HeaderMap? headers,
        ICollection<LebRowDto> rows,
        ref IReadOnlyList<LebSourceColumn>? sourceColumns)
    {
        if (values.All(string.IsNullOrWhiteSpace))
            return;

        if (IsHeader(values))
        {
            headers = CreateHeaderMap(values);
            sourceColumns ??= headers.Columns;
            return;
        }

        if (headers is null)
            return;

        rows.Add(new LebRowDto
        {
            RowNumber = rowNumber,
            SourceValues = headers.Columns.ToDictionary(
                column => column.EffectiveHeader,
                column => column.Index - 1 < values.Count
                    ? NullIfEmpty(values[column.Index - 1])
                    : null,
                StringComparer.OrdinalIgnoreCase),
            MunicipalityId = Get(values, headers, "GemID"),
            MunicipalityName = Get(values, headers, "Gemeinde"),
            BuildingId = Get(values, headers, "GebID"),
            BuildingName = Get(values, headers, "Gebäude"),
            ConstructionYear = Get(values, headers, "Baujahr"),
            FloorArea = Get(values, headers, "m2"),
            Year = Get(values, headers, "ReadingYear"),
            MeterId = Get(values, headers, "ZId"),
            MeterName = Get(values, headers, "Zähler"),
            Type = Get(values, headers, "Typ"),
            Unit = Get(values, headers, "Einheit"),
            SourceMedium = Get(values, headers, "Medium"),
            MeterGroup = Get(values, headers, "MGruppe"),
            MonthlyValues =
            [
                Get(values, headers, "Jan"), Get(values, headers, "Feb"),
                Get(values, headers, "Mrz"), Get(values, headers, "Apr"),
                Get(values, headers, "Mai"), Get(values, headers, "Jun"),
                Get(values, headers, "Jul"), Get(values, headers, "Aug"),
                Get(values, headers, "Sep"), Get(values, headers, "Okt"),
                Get(values, headers, "Nov"), Get(values, headers, "Dez")
            ],
            AnnualValue = Get(values, headers, "AnnualTotal")
        });
    }

    private static bool IsHeader(IReadOnlyList<string> values)
    {
        var set = values.Select(NormalizeHeader).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return set.Contains("GEMID") && set.Contains("GEBID") && set.Contains("ZID");
    }

    private static HeaderMap CreateHeaderMap(IReadOnlyList<string> values)
    {
        var columns = new List<LebSourceColumn>(values.Count);
        var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var generatedNumber = 0;

        for (var index = 0; index < values.Count; index++)
        {
            var original = NullIfEmpty(values[index]);
            var generated = original is null;
            string effective;
            if (generated)
            {
                effective = $"Tabelle{++generatedNumber}";
            }
            else
            {
                var normalized = NormalizeHeader(original);
                occurrences.TryGetValue(normalized, out var occurrence);
                occurrence++;
                occurrences[normalized] = occurrence;
                effective = normalized == "JAHR"
                    ? occurrence switch
                    {
                        1 => "ReadingYear",
                        2 => "AnnualTotal",
                        _ => $"Jahr{occurrence}"
                    }
                    : occurrence == 1
                        ? original!
                        : $"{original}{occurrence}";
            }

            columns.Add(new LebSourceColumn
            {
                Index = index + 1,
                OriginalHeader = original,
                EffectiveHeader = effective!,
                WasHeaderGenerated = generated
            });
        }

        var indexes = columns.ToDictionary(
            column => NormalizeHeader(column.EffectiveHeader),
            column => column.Index - 1,
            StringComparer.OrdinalIgnoreCase);
        var missing = RequiredHeaders
            .Where(header => !indexes.ContainsKey(NormalizeHeader(header)))
            .ToArray();
        if (missing.Length > 0)
            throw new InvalidImportFileException(
                $"LEB-Pflichtspalten fehlen: {string.Join(", ", missing)}.");

        return new HeaderMap(indexes, columns);
    }

    private static string? Get(
        IReadOnlyList<string> values,
        HeaderMap headers,
        string name)
    {
        if (!headers.Indexes.TryGetValue(NormalizeHeader(name), out var position) ||
            position >= values.Count)
            return null;
        return NullIfEmpty(values[position]);
    }

    private static string? NullIfEmpty(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeHeader(string? value) =>
        (value ?? string.Empty).Trim().TrimStart('\uFEFF').ToUpperInvariant();

    private static void EnsureData(IReadOnlyCollection<LebRowDto> rows)
    {
        if (rows.Count == 0)
            throw new InvalidImportFileException(
                "Die Datei enthält keine LEB-Datenzeilen oder keinen erkennbaren Header.");
    }

    private static LebWorkbookDto CreateWorkbook(
        IReadOnlyList<LebSourceColumn>? columns,
        IReadOnlyList<LebRowDto> rows)
    {
        var completedColumns = (columns ?? [])
            .Select(column =>
            {
                var values = rows
                    .Select(row => new
                    {
                        row.RowNumber,
                        Value = row.SourceValues.TryGetValue(
                            column.EffectiveHeader, out var value)
                            ? value
                            : null
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                    .Select(item => new LebSourceColumnValue
                    {
                        RowNumber = item.RowNumber,
                        Value = item.Value!
                    })
                    .ToList();
                return new LebSourceColumn
                {
                    Index = column.Index,
                    OriginalHeader = column.OriginalHeader,
                    EffectiveHeader = column.EffectiveHeader,
                    WasHeaderGenerated = column.WasHeaderGenerated,
                    HasData = values.Count > 0,
                    Values = values
                };
            })
            .ToList();

        return new LebWorkbookDto
        {
            Columns = completedColumns,
            Rows = rows
        };
    }

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    private static char DetectDelimiter(string line)
    {
        var delimiters = new[] { ';', ',', '\t' };
        return delimiters.OrderByDescending(delimiter => line.Count(x => x == delimiter)).First();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
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
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }
        values.Add(value.ToString());
        return values;
    }

    private sealed record HeaderMap(
        IReadOnlyDictionary<string, int> Indexes,
        IReadOnlyList<LebSourceColumn> Columns);
}
