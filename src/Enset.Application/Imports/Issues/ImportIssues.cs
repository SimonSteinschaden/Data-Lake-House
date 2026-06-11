namespace Enset.Application.Imports.Issues;

public class ImportIssue
{
    public ImportIssueType Type { get; set; }

    public ImportIssueSeverity Severity { get; set; }

    public string Message { get; set; } = string.Empty;

    public double? SimilarityScore { get; set; }

    public bool RequiresUserDecision { get; set; }
}