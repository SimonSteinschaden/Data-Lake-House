namespace Enset.Application.Quality;

public enum MeterProfileAnalysisStatus
{
    NotAnalyzed, AnalysisCompleted, RequiresReview, Curated, Confirmed
}

public sealed record MeterProfileAnalysisIssue(
    string Code, string Severity, DateTime? Timestamp, string Message);

public sealed record MeterProfileAnalysisResult(
    Guid AnalysisId, Guid MeterId, DateTime? PeriodFrom, DateTime? PeriodTo,
    long ExpectedReadingCount, long ActualReadingCount,
    decimal CompletenessPercentage, long GapCount, long AnomalyCount,
    long BlockingIssueCount, long WarningCount, string AnalysisVersion,
    DateTime ExecutedAtUtc, Guid? ExecutedByUserId,
    MeterProfileAnalysisStatus Status, string Summary,
    IReadOnlyList<MeterProfileAnalysisIssue> DetailIssues);

public static class MeterProfileQuality
{
    public static Domain.Curation.DataMaturityLevel Evaluate(
        MeterProfileAnalysisResult? analysis)
    {
        if (analysis is null ||
            analysis.Status == MeterProfileAnalysisStatus.NotAnalyzed)
            return Domain.Curation.DataMaturityLevel.Bronze;
        return analysis.Status == MeterProfileAnalysisStatus.Confirmed &&
               analysis.BlockingIssueCount == 0
            ? Domain.Curation.DataMaturityLevel.Gold
            : Domain.Curation.DataMaturityLevel.Silver;
    }
}
