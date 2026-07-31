using System.Text;
using Enset.Infrastructure.Imports.MassImport;
using Xunit;

namespace Enset.Import.Tests;

public sealed class MeterReadingMassImportTests
{
    [Fact]
    public async Task StreamingReader_YieldsConfiguredChunks()
    {
        await using var stream = Csv(25);
        var chunks = new List<IReadOnlyList<MeterReadingStagingRow>>();

        await foreach (var chunk in new MeterReadingStreamingReader()
                           .ReadChunks(stream, Guid.NewGuid(), null, 10, default))
            chunks.Add(chunk);

        Assert.Equal([10, 10, 5], chunks.Select(x => x.Count));
        Assert.Equal(25, chunks.Sum(x => x.Count));
    }

    [Fact]
    public async Task StreamingReader_PreservesInvalidRowInformation()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "MeterNumber;Timestamp;Value;Unit\n" +
            "M-1;not-a-date;not-a-number;kWh\n"));

        var row = Assert.Single(await ReadAll(stream));

        Assert.Equal(2, row.SourceRowNumber);
        Assert.Equal("TIMESTAMP_INVALID", row.ValidationCode);
        Assert.NotEmpty(row.RawSourceHash);
    }

    [Fact]
    public async Task StreamingReader_ObservesCancellation()
    {
        await using var stream = Csv(2);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in new MeterReadingStreamingReader()
                               .ReadChunks(
                                   stream, null, "M-1", 1,
                                   cancellation.Token))
            {
            }
        });
    }

    [Fact]
    public void StreamingPath_HasNoWholeFileMaterialization()
    {
        var source = File.ReadAllText(SourcePath(
            "src", "Enset.Infrastructure", "Imports", "MassImport",
            "MeterReadingStreamingReader.cs"));

        Assert.DoesNotContain("ReadAllLines", source);
        Assert.DoesNotContain("ReadToEnd", source);
        Assert.DoesNotContain(".ToList(", source);
    }

    [Fact]
    public void MeterReading_IsExcludedFromGenericAudit()
    {
        var source = File.ReadAllText(SourcePath(
            "src", "Enset.Infrastructure", "Persistence",
            "EnsetDbContext.cs"));

        Assert.Contains("x.Entity is not MeterReading", source);
    }

    [Fact]
    public void MassImport_UsesBinaryCopyAndExceptionBoundary()
    {
        var root = SourcePath(
            "src", "Enset.Infrastructure", "Imports", "MassImport");
        var processor = File.ReadAllText(Path.Combine(
            root, "MeterReadingMassImportProcessor.cs"));
        var worker = File.ReadAllText(Path.Combine(
            root, "MeterReadingMassImportBackgroundService.cs"));

        Assert.Contains("BeginBinaryImportAsync", processor);
        Assert.DoesNotContain("SaveChanges per", processor);
        Assert.Contains("catch (Exception exception)", worker);
    }

    [Fact]
    public async Task StreamingAnalyzer_AggregatesAndKeepsOnlySamples()
    {
        var path = Path.Combine(
            Path.GetTempPath(), $"enset-analysis-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(
                path,
                Encoding.UTF8.GetString(Csv(105).ToArray()));
            var analyzer = new MeterReadingStreamingAnalyzer(
                new MeterReadingStreamingReader());

            var result = await analyzer.Analyze(
                path, "M-1", 10, default);

            Assert.Equal(105, result.Summary.ReadRows);
            Assert.Equal(105, result.Summary.ValidRows);
            Assert.Equal(20, result.Samples.Count);
            Assert.Contains("Timestamp", result.Summary.Headers);
            Assert.Empty(result.Issues);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void InteractiveCsvAnalysis_DoesNotUseMaterializingReader()
    {
        var source = File.ReadAllText(SourcePath(
            "src", "Enset.Infrastructure", "Imports", "Analysis",
            "ExcelImportAnalysisService.cs"));

        Assert.DoesNotContain("new CsvImportReader", source);
        Assert.Contains("_streamingAnalyzer.Analyze", source);
    }

    private static async Task<List<MeterReadingStagingRow>> ReadAll(
        Stream stream)
    {
        var rows = new List<MeterReadingStagingRow>();
        await foreach (var chunk in new MeterReadingStreamingReader()
                           .ReadChunks(stream, null, "M-1", 10, default))
            rows.AddRange(chunk);
        return rows;
    }

    private static MemoryStream Csv(int rows)
    {
        var text = new StringBuilder(
            "MeterNumber;Timestamp;Value;Unit;ReadingType;QualityFlag\n");
        for (var index = 0; index < rows; index++)
            text.AppendLine(
                $"M-1;2025-01-01T00:{index % 60:00}:00Z;{index};kWh;" +
                "IntervalValue;Measured");
        return new MemoryStream(Encoding.UTF8.GetBytes(text.ToString()));
    }

    private static string SourcePath(params string[] segments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null &&
               !(Directory.Exists(Path.Combine(current.FullName, "src")) &&
                 Directory.Exists(Path.Combine(current.FullName, "tests"))))
            current = current.Parent;
        Assert.NotNull(current);
        return Path.Combine([current.FullName, .. segments]);
    }
}
