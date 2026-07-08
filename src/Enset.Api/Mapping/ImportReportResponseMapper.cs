using Enset.Api.Contracts.Imports.Responses;
using Enset.Application.Imports.Reports;

namespace Enset.Api.Mapping;

public static class ImportReportResponseMapper
{
    public static ImportReportResponseDto ToResponse(this ImportReport report)
    {
        return new ImportReportResponseDto
        {
            ImportId = report.ImportId,
            Status = report.Status,
            SourceFile = report.SourceFile is null
                ? null
                : new ImportSourceFileResponseDto
                {
                    FileName = report.SourceFile.FileName,
                    ContentType = report.SourceFile.ContentType,
                    Length = report.SourceFile.Length,
                    Sha256 = report.SourceFile.Sha256,
                    IsRawArchived = !string.IsNullOrWhiteSpace(report.SourceFile.RawStoragePath)
                },
            Customers = report.Customers,
            Issues = report.Issues.Select(issue => new ImportIssueResponseDto
            {
                IssueId = issue.IssueId,
                EntityId = issue.EntityId,
                Type = issue.Type,
                Severity = issue.Severity,
                Message = issue.Message,
                SimilarityScore = issue.SimilarityScore,
                RequiresUserDecision = issue.RequiresUserDecision,
                FieldName = issue.FieldName,
                FirstValue = issue.FirstValue,
                SecondValue = issue.SecondValue,
                ResolutionAction = issue.ResolutionAction,
                CustomResolvedValue = issue.CustomResolvedValue,
                IsResolved = issue.IsResolved
            }).ToList(),
            AuditTrail = report.AuditTrail.Select(entry => new ImportAuditEntryResponseDto
            {
                AuditId = entry.AuditId,
                Timestamp = entry.Timestamp,
                UserId = entry.UserId,
                Action = entry.Action,
                IssueId = entry.IssueId,
                PreviousResolutionAction = entry.PreviousResolutionAction,
                ResolutionAction = entry.ResolutionAction,
                PreviousCustomResolvedValue = entry.PreviousCustomResolvedValue,
                CustomResolvedValue = entry.CustomResolvedValue,
                Details = entry.Details
            }).ToList(),
            Decision = report.Decision,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            CustomerCount = report.CustomerCount,
            BuildingCount = report.BuildingCount,
            IssueCount = report.IssueCount,
            ErrorCount = report.ErrorCount,
            WarningCount = report.WarningCount
        };
    }
}
