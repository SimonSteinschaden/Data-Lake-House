using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Reports;

public class ImportReport
{
    public Guid ImportId { get; init; } = Guid.NewGuid();

    public Guid? CreatedByUserId { get; set; }

    public Guid? CustomerId { get; set; }

    public ImportStatus Status { get; set; } = ImportStatus.Pending;

    public ImportSourceType SourceType { get; set; } = ImportSourceType.CRM_Excel;

    public string? DefaultMeterNumber { get; set; }
    public Guid? AssignedMeterId { get; set; }
    public CsvMeterReadingMapping? CsvMapping { get; set; }

    public ImportSourceFileMetadata? SourceFile { get; set; }

    public List<ImportIssue> Issues { get; init; } = [];

    public IReadOnlyList<CustomerImportDto> Customers { get; set; } = [];

    public IReadOnlyList<BuildingImportDto> Buildings { get; set; } = [];

    public IReadOnlyList<MeterImportDto> Meters { get; set; } = [];

    public IReadOnlyList<MeterReadingImportDto> MeterReadings { get; set; } = [];

    public IReadOnlyList<LebSourceColumn> SourceColumns { get; set; } = [];

    public List<ImportResolutionRule> ResolutionRules { get; init; } = [];

    public List<ImportAuditEntry> AuditTrail { get; init; } = [];

    public ImportDecision Decision { get; set; } = new();

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CustomerCount { get; init; }

    public int BuildingCount { get; init; }

    public int MeterCount { get; init; }

    public int MeterReadingCount { get; init; }

    public int IssueCount => Issues.Count;

    public int ErrorCount => Errors.Count;

    public int WarningCount => Warnings.Count;

    public bool HasErrors => Errors.Any();

    public bool HasWarnings => Warnings.Any();

    public bool HasOpenCommitBlockingIssues =>
        Issues.Any(issue => issue.IsCommitBlocking);

    public int UnresolvedIssueCount =>
        Issues.Count(issue => issue.IsCommitBlocking);

    public int OpenIssueCount =>
        Issues.Count(issue => !issue.IsResolved);

    public int BlockingOpenIssueCount =>
        Issues.Count(issue => issue.IsCommitBlocking);

    public int AutomaticallyResolvedIssueCount =>
        Issues.Count(issue =>
            issue.IsResolved &&
            issue.ResolutionSource == ImportResolutionSource.Automatic);

    public int ManuallyResolvedIssueCount =>
        Issues.Count(issue =>
            issue.IsResolved &&
            issue.ResolutionSource == ImportResolutionSource.Manual);

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

    public void RecalculateCommitReadiness()
    {
        var hasBlockingIssues = HasOpenCommitBlockingIssues;

        Decision = ImportDecisionEngine.Decide(this);

        Status = hasBlockingIssues
            ? ImportStatus.AwaitingResolution
            : ImportStatus.ReadyToCommit;
    }
}
