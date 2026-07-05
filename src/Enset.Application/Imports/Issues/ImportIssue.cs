
namespace Enset.Application.Imports.Issues;

public class ImportIssue
{
    public ImportIssueType Type { get; set; }

    public ImportIssueSeverity Severity { get; set; }

    public string Message { get; set; } = string.Empty;

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