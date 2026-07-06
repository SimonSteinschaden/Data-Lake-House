using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Reports;

public class ImportReport
{
    public Guid ImportId { get; init; } = Guid.NewGuid();

    public IReadOnlyList<CustomerImportDto> Customers { get; set; } = [];

    public List<ImportIssue> Issues { get; } = [];

    public ImportDecision Decision { get; set; } = new();

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public int CustomerCount => Customers.Count;

    public int BuildingCount { get; init; }

    public int IssueCount => Issues.Count;

    public int ErrorCount => Errors.Count;

    public int WarningCount => Warnings.Count;

    public bool HasErrors => Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public IReadOnlyList<ImportIssue> Errors =>
        Issues
            .Where(i => i.Severity >= ImportIssueSeverity.Error)
            .ToList();

    public IReadOnlyList<ImportIssue> Warnings =>
        Issues
            .Where(i => i.Severity == ImportIssueSeverity.Warning)
            .ToList();

    public IReadOnlyList<ImportIssue> Informations =>
        Issues
            .Where(i => i.Severity == ImportIssueSeverity.Info)
            .ToList();

    public IReadOnlyList<ImportIssue> CriticalIssues =>
        Issues
            .Where(i => i.Severity == ImportIssueSeverity.Critical)
            .ToList();
}
