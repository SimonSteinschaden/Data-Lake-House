using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class ImportReportResponseDto
{
    public Guid ImportId { get; init; }

    public ImportStatus Status { get; init; }

    public ImportSourceFileResponseDto? SourceFile { get; init; }

    public IReadOnlyList<CustomerImportDto> Customers { get; init; } = [];

    public IReadOnlyList<ImportIssueResponseDto> Issues { get; init; } = [];

    public IReadOnlyList<ImportAuditEntryResponseDto> AuditTrail { get; init; } = [];

    public ImportDecision Decision { get; init; } = new();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int CustomerCount { get; init; }

    public int BuildingCount { get; init; }

    public int IssueCount { get; init; }

    public int ErrorCount { get; init; }

    public int WarningCount { get; init; }
}
