using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Resolution;

namespace Enset.Api.Contracts.Imports.Responses;

public sealed class ImportIssueResponse
{
    public Guid IssueId { get; init; }

    public Guid? EntityId { get; init; }

    public ImportIssueType Type { get; init; }

    public ImportIssueSeverity Severity { get; init; }

    public string Message { get; init; } = string.Empty;

    public double? SimilarityScore { get; init; }

    public bool RequiresUserDecision { get; init; }

    public string? FieldName { get; init; }

    public int? SourceRowNumber { get; init; }

    public string? FirstValue { get; init; }

    public string? SecondValue { get; init; }

    public ImportIssueValuePattern ValuePattern { get; init; }

    public ResolutionTargetDataType TargetDataType { get; init; }

    public NumberFormatPattern NumberFormatPattern { get; init; }

    public IReadOnlyList<string> ExampleValues { get; init; } = [];

    public int MatchingIssueCount { get; init; }

    public int CompatibleIssueTypeCount { get; init; }

    public bool SupportsGroupResolution { get; init; }

    public IReadOnlyList<ResolutionScope> SupportedScopes { get; init; } = [];

    public IReadOnlyList<AllowedImportResolutionResponse> AllowedResolutions
        { get; init; } = [];

    public ImportResolutionAction ResolutionAction { get; init; }

    public string? CustomResolvedValue { get; init; }

    public bool IsResolved { get; init; }

    public ImportResolutionSource ResolutionSource { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public string? ResolvedBy { get; init; }

    public ResolutionScope? ResolutionScope { get; init; }

    public Guid? ResolutionRuleId { get; init; }
}
