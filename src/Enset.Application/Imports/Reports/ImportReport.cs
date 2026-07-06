using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Reports;

public class ImportReport
{
    public int CustomerCount { get; init; }

    public int BuildingCount { get; init; }

    public List<ImportIssue> Issues { get; } = [];

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