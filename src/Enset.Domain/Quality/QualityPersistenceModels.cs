namespace Enset.Domain.Quality;

public enum QualityActorType { System, EnsetEmployee, EnsetAdministrator }
public enum ProfileAnalysisStatus { NotAnalyzed, Running, Completed, RequiresReview, Curated, Confirmed, Failed, Cancelled }
public enum ProfileIssueCategory { MissingTimestamp, Gap, Duplicate, InvalidTimestamp, MixedInterval, UnknownUnit, NegativeValue, ImplausibleValue, Outlier, ZeroSequence, SuddenJump, IncompletePeriod, MissingAssignment, Other }
public enum ProfileIssueSeverity { Information, Warning, Blocking }
public enum ProfileIssueResolutionStatus { Open, UnderReview, Accepted, Corrected, Estimated, IgnoredWithReason, Resolved }
public enum ProfileDecisionType { ConfirmAsCorrect, CorrectValue, MarkInvalid, AcceptGap, AddManualValue, GenerateEstimatedValue, MarkForObservation, IgnoreWithReason, Reopen }

public sealed class BuildingInventoryDeclaration
{
    public Guid Id { get; set; }=Guid.NewGuid(); public Guid BuildingId { get; set; }
    public bool MeterInventoryComplete { get; set; } public bool EnergySystemInventoryComplete { get; set; }
    public bool NoRelevantEnergySystemsConfirmed { get; set; } public Guid ConfirmedByUserId { get; set; }
    public string? ConfirmedByDisplayNameSnapshot { get; set; } public DateTime ConfirmedAtUtc { get; set; }
    public string? Comment { get; set; } public DateTime? InvalidatedAtUtc { get; set; }
    public Guid? InvalidatedByUserId { get; set; } public string? InvalidationReason { get; set; }
    public int VersionNumber { get; set; } public bool IsCurrent { get; set; }
    public DateTime CreatedAtUtc { get; set; } public uint RowVersion { get; set; }
}

public sealed class MeterProfileAnalysis
{
    public Guid Id { get; set; }=Guid.NewGuid(); public Guid MeterId { get; set; }
    public int VersionNumber { get; set; } public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; } public ProfileAnalysisStatus AnalysisStatus { get; set; }
    public string AnalysisVersion { get; set; }=""; public long ExpectedReadingCount { get; set; }
    public long ActualReadingCount { get; set; } public decimal CompletenessPercentage { get; set; }
    public long GapCount { get; set; } public long AnomalyCount { get; set; }
    public long BlockingIssueCount { get; set; } public long WarningCount { get; set; }
    public int? DetectedIntervalSeconds { get; set; } public string? DetectedUnit { get; set; }
    public DateTime ExecutedAtUtc { get; set; } public QualityActorType ExecutedByActorType { get; set; }
    public Guid? ExecutedByUserId { get; set; } public string? Summary { get; set; }
    public string? ExecutedByDisplayNameSnapshot { get; set; }
    public bool IsCurrent { get; set; } public DateTime? SupersededAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } public uint RowVersion { get; set; }
}

public sealed class MeterProfileIssue
{
    public Guid Id { get; set; }=Guid.NewGuid(); public Guid MeterProfileAnalysisId { get; set; }
    public Guid MeterId { get; set; } public string Code { get; set; }="";
    public ProfileIssueCategory Category { get; set; } public ProfileIssueSeverity Severity { get; set; }
    public DateTime? TimestampFromUtc { get; set; } public DateTime? TimestampToUtc { get; set; }
    public long? SourceRowNumber { get; set; } public string? OriginalValue { get; set; }
    public string? ExpectedValue { get; set; } public string Message { get; set; }="";
    public string? TechnicalDetails { get; set; } public bool IsBlocking { get; set; }
    public ProfileIssueResolutionStatus ResolutionStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; } public DateTime? ResolvedAtUtc { get; set; }
    public uint RowVersion { get; set; }
}

public sealed class MeterProfileCurationDecision
{
    public Guid Id { get; set; }=Guid.NewGuid(); public Guid MeterProfileIssueId { get; set; }
    public Guid MeterProfileAnalysisId { get; set; } public Guid MeterId { get; set; }
    public ProfileDecisionType DecisionType { get; set; } public string? PreviousValue { get; set; }
    public string? NewValue { get; set; } public string? GeneratedValueMethod { get; set; }
    public int? ConfidencePercent { get; set; } public string Reason { get; set; }="";
    public string? Comment { get; set; } public Guid DecidedByUserId { get; set; }
    public string? DecidedByDisplayNameSnapshot { get; set; } public DateTime DecidedAtUtc { get; set; }
    public bool IsFinal { get; set; } public Guid? SupersedesDecisionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } public uint RowVersion { get; set; }
}
