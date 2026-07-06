using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class ImportIssueResponseDto
{
    public Guid IssueId { get; init; }

    public Guid? EntityId { get; init; }

    public ImportIssueType Type { get; init; }

    public ImportIssueSeverity Severity { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool RequiresUserDecision { get; init; }

    public string? FieldName { get; init; }

    public string? FirstValue { get; init; }

    public string? SecondValue { get; init; }

    public ImportResolutionAction ResolutionAction { get; init; }

    public string? CustomResolvedValue { get; init; }

    public bool IsResolved { get; init; }
}
