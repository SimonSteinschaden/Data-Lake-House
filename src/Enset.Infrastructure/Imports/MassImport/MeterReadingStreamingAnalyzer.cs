using System.Text;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Domain.Energy;

namespace Enset.Infrastructure.Imports.MassImport;

public sealed record MeterReadingStreamingAnalysis(
    MeterReadingAnalysisSummary Summary,
    IReadOnlyList<MeterReadingImportDto> Samples,
    IReadOnlyList<ImportIssue> Issues);

public interface IMeterReadingStreamingAnalyzer
{
    Task<MeterReadingStreamingAnalysis> Analyze(
        string filePath,
        string? defaultMeterNumber,
        int chunkSize,
        CancellationToken cancellationToken);
}

public sealed class MeterReadingStreamingAnalyzer(
    IMeterReadingStreamingReader reader)
    : IMeterReadingStreamingAnalyzer
{
    private const int SampleLimit = 20;

    public async Task<MeterReadingStreamingAnalysis> Analyze(
        string filePath,
        string? defaultMeterNumber,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var headers = await ReadHeaders(filePath, cancellationToken);
        var samples = new List<MeterReadingImportDto>(SampleLimit);
        var issueCounts = new Dictionary<string, (long Count, long FirstRow)>(
            StringComparer.Ordinal);
        var intervals = new Dictionary<int, long>();
        long read = 0;
        long valid = 0;
        DateTime? start = null;
        DateTime? end = null;
        DateTime? previous = null;

        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await foreach (var chunk in reader.ReadChunks(
                           stream, null, defaultMeterNumber, chunkSize,
                           cancellationToken))
        {
            foreach (var row in chunk)
            {
                read++;
                if (samples.Count < SampleLimit)
                    samples.Add(ToSample(row));
                if (row.ValidationCode is not null)
                {
                    AddIssue(issueCounts, row.ValidationCode,
                        row.SourceRowNumber);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(row.Unit))
                {
                    AddIssue(issueCounts, "UNIT_MISSING",
                        row.SourceRowNumber);
                    continue;
                }
                if (!row.MeterId.HasValue &&
                    string.IsNullOrWhiteSpace(row.MeterNumber))
                {
                    AddIssue(issueCounts, "METER_MISSING",
                        row.SourceRowNumber);
                    continue;
                }

                valid++;
                if (row.Timestamp is not { } timestamp)
                    continue;
                start = start is null || timestamp < start
                    ? timestamp : start;
                end = end is null || timestamp > end
                    ? timestamp : end;
                if (previous is not null)
                {
                    var seconds = (int)(timestamp - previous.Value)
                        .TotalSeconds;
                    if (seconds > 0)
                        intervals[seconds] =
                            intervals.GetValueOrDefault(seconds) + 1;
                }
                previous = timestamp;
            }
        }
        if (read == 0)
            throw new InvalidDataException(
                "The CSV file contains no data rows.");

        var issues = issueCounts.Select(x => CreateIssue(
            x.Key, x.Value.Count, x.Value.FirstRow)).ToList();
        var interval = intervals
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => (int?)x.Key)
            .FirstOrDefault();
        return new(
            new MeterReadingAnalysisSummary
            {
                Headers = headers,
                ReadRows = read,
                ValidRows = valid,
                InvalidRows = read - valid,
                PeriodStart = start,
                PeriodEnd = end,
                IntervalSeconds = interval
            },
            samples,
            issues);
    }

    private static void AddIssue(
        IDictionary<string, (long Count, long FirstRow)> issues,
        string code,
        long row)
    {
        issues.TryGetValue(code, out var current);
        issues[code] = current.Count == 0
            ? (1, row)
            : (current.Count + 1, current.FirstRow);
    }

    private static ImportIssue CreateIssue(
        string code,
        long count,
        long firstRow)
    {
        var type = code switch
        {
            "TIMESTAMP_INVALID" or "TIMESTAMP_COLUMN_MISSING" =>
                ImportIssueType.InvalidTimestamp,
            "VALUE_INVALID" or "VALUE_COLUMN_MISSING" =>
                ImportIssueType.InvalidValue,
            "METER_MISSING" => ImportIssueType.AssignMeterRequired,
            _ => ImportIssueType.MissingData
        };
        return new ImportIssue
        {
            Type = type,
            Severity = ImportIssueSeverity.Error,
            RequiresUserDecision = type ==
                ImportIssueType.AssignMeterRequired,
            FieldName = code,
            SourceRowNumber = firstRow > int.MaxValue
                ? null : (int)firstRow,
            Message = $"{count} CSV row(s) failed validation ({code})."
        };
    }

    private static MeterReadingImportDto ToSample(
        MeterReadingStagingRow row) =>
        new()
        {
            MeterId = row.MeterId,
            MeterNumber = row.MeterNumber ?? string.Empty,
            MeterNumberRaw = row.MeterNumber,
            Timestamp = row.Timestamp,
            Value = row.Value,
            Unit = row.Unit,
            RowNumber = row.SourceRowNumber > int.MaxValue
                ? null : (int)row.SourceRowNumber,
            HasError = row.ValidationCode is not null ||
                       string.IsNullOrWhiteSpace(row.Unit),
            ErrorMessage = row.ValidationMessage,
            ReadingType = Enum.TryParse<MeterReadingType>(
                row.ReadingType, true, out var type)
                ? type : MeterReadingType.Unknown,
            IntervalSeconds = row.IntervalSeconds
        };

    private static async Task<IReadOnlyList<string>> ReadHeaders(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var text = new StreamReader(
            stream, Encoding.UTF8, true, 81920);
        var line = await text.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(line))
            throw new InvalidDataException("CSV header is missing.");
        var delimiter = new[] { ';', ',', '\t' }
            .OrderByDescending(x => line.Count(c => c == x))
            .First();
        return line.Split(delimiter)
            .Select(x => x.Trim().Trim('"'))
            .ToArray();
    }
}
