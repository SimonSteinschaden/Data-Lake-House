using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Infrastructure.Imports.Persistence.Entities;

namespace Enset.Infrastructure.Imports.Persistence.Mappings;

public static class ImportReportPersistenceMapper
{
    public static ImportReportEntity ToEntity(ImportReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new ImportReportEntity
        {
            ImportId = report.ImportId,
            Status = report.Status.ToString(),
            SourceFileName = report.SourceFile?.FileName,
            SourceFileContentType = report.SourceFile?.ContentType,
            SourceFileSizeBytes = report.SourceFile?.Length,
            SourceFileSha256 = report.SourceFile?.Sha256,
            SourceFileStagedPath = report.SourceFile?.StagedPath,
            SourceFileRawStoragePath = report.SourceFile?.RawStoragePath,
            CustomerCount = report.CustomerCount,
            BuildingCount = report.BuildingCount,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            Issues = report.Issues
                .Select(issue => ToIssueEntity(report.ImportId, issue))
                .ToList(),
            AuditTrail = report.AuditTrail
                .Select(audit => ToAuditEntity(report.ImportId, audit))
                .ToList()
        };
    }

    public static ImportReport ToModel(ImportReportEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var report = new ImportReport
        {
            ImportId = entity.ImportId,
            Status = ParseEnum(entity.Status, ImportStatus.Pending),
            SourceFile = CreateSourceFileMetadata(entity),
            CustomerCount = entity.CustomerCount,
            BuildingCount = entity.BuildingCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        report.Issues.AddRange(
            entity.Issues
                .OrderBy(issue => issue.IssueId)
                .Select(ToIssueModel));

        report.AuditTrail.AddRange(
            entity.AuditTrail
                .OrderBy(audit => audit.Timestamp)
                .ThenBy(audit => audit.AuditId)
                .Select(ToAuditModel));

        return report;
    }

    private static ImportIssueEntity ToIssueEntity(
        Guid importId,
        ImportIssue issue)
    {
        return new ImportIssueEntity
        {
            IssueId = issue.IssueId,
            ImportId = importId,
            EntityId = issue.EntityId,
            Type = issue.Type.ToString(),
            Severity = issue.Severity.ToString(),
            Message = issue.Message,
            SimilarityScore = issue.SimilarityScore,
            RequiresUserDecision = issue.RequiresUserDecision,
            FieldName = issue.FieldName,
            FirstValue = issue.FirstValue,
            SecondValue = issue.SecondValue,
            ResolutionAction = issue.ResolutionAction.ToString(),
            CustomResolvedValue = issue.CustomResolvedValue,
            IsResolved = issue.IsResolved
        };
    }

    private static ImportAuditEntryEntity ToAuditEntity(
        Guid importId,
        ImportAuditEntry audit)
    {
        return new ImportAuditEntryEntity
        {
            AuditId = audit.AuditId,
            ImportId = importId,
            Timestamp = audit.Timestamp,
            UserId = audit.UserId,
            Action = audit.Action,
            IssueId = audit.IssueId,
            PreviousResolutionAction = audit.PreviousResolutionAction?.ToString(),
            ResolutionAction = audit.ResolutionAction?.ToString(),
            PreviousCustomResolvedValue = audit.PreviousCustomResolvedValue,
            CustomResolvedValue = audit.CustomResolvedValue,
            Details = audit.Details
        };
    }

    private static ImportIssue ToIssueModel(ImportIssueEntity entity)
    {
        return new ImportIssue
        {
            IssueId = entity.IssueId,
            EntityId = entity.EntityId,
            Type = ParseEnum(entity.Type, ImportIssueType.InvalidValue),
            Severity = ParseEnum(entity.Severity, ImportIssueSeverity.Error),
            Message = entity.Message,
            SimilarityScore = entity.SimilarityScore,
            RequiresUserDecision = entity.RequiresUserDecision,
            FieldName = entity.FieldName,
            FirstValue = entity.FirstValue,
            SecondValue = entity.SecondValue,
            ResolutionAction = ParseEnum(
                entity.ResolutionAction,
                ImportResolutionAction.None),
            CustomResolvedValue = entity.CustomResolvedValue,
            IsResolved = entity.IsResolved
        };
    }

    private static ImportAuditEntry ToAuditModel(ImportAuditEntryEntity entity)
    {
        return new ImportAuditEntry
        {
            AuditId = entity.AuditId,
            Timestamp = entity.Timestamp,
            UserId = entity.UserId,
            Action = entity.Action,
            IssueId = entity.IssueId,
            PreviousResolutionAction = ParseNullableEnum<ImportResolutionAction>(
                entity.PreviousResolutionAction),
            ResolutionAction = ParseNullableEnum<ImportResolutionAction>(
                entity.ResolutionAction),
            PreviousCustomResolvedValue = entity.PreviousCustomResolvedValue,
            CustomResolvedValue = entity.CustomResolvedValue,
            Details = entity.Details
        };
    }

    private static ImportSourceFileMetadata? CreateSourceFileMetadata(
        ImportReportEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.SourceFileName))
            return null;

        return new ImportSourceFileMetadata
        {
            FileName = entity.SourceFileName,
            ContentType = entity.SourceFileContentType,
            Length = entity.SourceFileSizeBytes ?? 0,
            Sha256 = entity.SourceFileSha256 ?? string.Empty,
            StagedPath = entity.SourceFileStagedPath ?? string.Empty,
            RawStoragePath = entity.SourceFileRawStoragePath
        };
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
