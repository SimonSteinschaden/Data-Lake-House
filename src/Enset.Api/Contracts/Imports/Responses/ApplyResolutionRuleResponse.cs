using Enset.Application.Imports.Enums;

namespace Enset.Api.Contracts.Imports.Responses;

public sealed class ApplyResolutionRuleResponse
{
    public Guid RuleId { get; init; }
    public int MatchedIssueCount { get; init; }
    public int ResolvedIssueCount { get; init; }
    public int FailedIssueCount { get; init; }
    public int SkippedIssueCount { get; init; }
    public int RemainingBlockingIssueCount { get; init; }
    public ImportStatus Status { get; init; }
    public ImportReportResponse Report { get; init; } = new();
}
