using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public sealed class ImportResolutionRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ImportId { get; init; }
    public ImportSourceType SourceType { get; init; }
    public ImportIssueType IssueCode { get; init; }
    public string? FieldName { get; init; }
    public ImportIssueValuePattern ValuePattern { get; init; }
    public ResolutionTargetDataType TargetDataType { get; init; }
    public NumberFormatPattern NumberFormatPattern { get; init; }
    public string? MatchValue { get; init; }
    public ImportResolutionType ResolutionType { get; init; }
    public ImportResolutionAction ResolutionAction { get; init; }
    public string? ResolutionPayload { get; init; }
    public ResolutionScope Scope { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public int MatchedIssueCount { get; set; }
    public int ResolvedIssueCount { get; set; }
    public int FailedIssueCount { get; set; }
    public int SkippedIssueCount { get; set; }
}
