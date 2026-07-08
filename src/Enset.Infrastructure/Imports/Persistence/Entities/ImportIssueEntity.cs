namespace Enset.Infrastructure.Imports.Persistence.Entities;

public sealed class ImportIssueEntity
{
    public Guid IssueId { get; set; }

    public Guid ImportId { get; set; }

    public ImportReportEntity ImportReport { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public double? SimilarityScore { get; set; }

    public bool RequiresUserDecision { get; set; }

    public string? FieldName { get; set; }

    public string? FirstValue { get; set; }

    public string? SecondValue { get; set; }

    public string ResolutionAction { get; set; } = string.Empty;

    public string? CustomResolvedValue { get; set; }

    public bool IsResolved { get; set; }
}
