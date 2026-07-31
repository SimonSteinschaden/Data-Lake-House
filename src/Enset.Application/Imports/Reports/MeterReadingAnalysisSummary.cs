namespace Enset.Application.Imports.Reports;

public sealed class MeterReadingAnalysisSummary
{
    public IReadOnlyList<string> Headers { get; init; } = [];
    public long ReadRows { get; init; }
    public long ValidRows { get; init; }
    public long InvalidRows { get; init; }
    public long DuplicateRows { get; init; }
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }
    public int? IntervalSeconds { get; init; }
}
