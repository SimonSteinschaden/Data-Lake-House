
namespace Enset.Application.Imports.Issues;

public class ImportIssue
{
    public Guid? EntityId { get; init; }

    public ImportIssueType Type { get; init; }

    public ImportIssueSeverity Severity { get; init; }

    public string Message { get; init; } = string.Empty;

    public double? SimilarityScore { get; set; }

    public bool RequiresUserDecision { get; set; }

    // Duplication Resolution

    public string? FieldName { get; set; }

    public string? FirstValue { get; set; }

    public string? SecondValue { get; set; }

    public ImportResolutionAction ResolutionAction { get; set; }
        = ImportResolutionAction.None;

    public string? CustomResolvedValue { get; set; }

    public bool IsResolved { get; set; }
}