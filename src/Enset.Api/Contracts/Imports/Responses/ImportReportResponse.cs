using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;

namespace Enset.Api.Contracts.Imports.Responses;

public sealed class ImportReportResponse
{
    public Guid ImportId { get; init; }

    public ImportStatus Status { get; init; }

    public ImportSourceFileResponse? SourceFile { get; init; }

    public IReadOnlyList<CustomerImportDto> Customers { get; init; } = [];

    public IReadOnlyList<ImportIssueResponse> Issues { get; init; } = [];

    public IReadOnlyList<ImportAuditEntryResponse> AuditTrail { get; init; } = [];

    public ImportDecision Decision { get; init; } = new();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int CustomerCount { get; init; }

    public int BuildingCount { get; init; }

    public int IssueCount { get; init; }

    public int ErrorCount { get; init; }

    public int WarningCount { get; init; }
}
