using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Resolution;

public sealed class ApplyResolutionService : IApplyResolutionService
{
    public ImportReport Apply(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        string userId,
        DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(resolutions);

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A user id is required.", nameof(userId));

        EnsureUniqueResolutions(resolutions);

        if (report.Status is ImportStatus.Committing or ImportStatus.Committed)
        {
            throw new InvalidOperationException(
                $"Import '{report.ImportId}' can no longer be changed in status '{report.Status}'.");
        }

        var issuesById = report.Issues.ToDictionary(issue => issue.IssueId);

        foreach (var resolution in resolutions)
        {
            if (!issuesById.TryGetValue(resolution.IssueId, out var issue))
            {
                throw new ArgumentException(
                    $"Issue '{resolution.IssueId}' does not belong to import '{report.ImportId}'.",
                    nameof(resolutions));
            }

            ApplyResolution(report, issue, resolution, userId, timestamp);
        }

        report.Decision = DetermineDecision(report.Issues);
        report.Status = DetermineStatus(report.Issues);
        report.UpdatedAt = timestamp;

        return report;
    }

    private static void EnsureUniqueResolutions(
        IReadOnlyCollection<ImportIssueResolution> resolutions)
    {
        var duplicateIssueId = resolutions
            .GroupBy(resolution => resolution.IssueId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateIssueId is not null)
        {
            throw new ArgumentException(
                $"More than one resolution was supplied for issue '{duplicateIssueId}'.",
                nameof(resolutions));
        }
    }

    private static void ApplyResolution(
        ImportReport report,
        ImportIssue issue,
        ImportIssueResolution resolution,
        string userId,
        DateTime timestamp)
    {
        if (!issue.RequiresUserDecision)
        {
            throw new InvalidOperationException(
                $"Issue '{issue.IssueId}' does not accept a user resolution.");
        }

        if (resolution.ResolutionAction == ImportResolutionAction.UseCustomValue &&
            string.IsNullOrWhiteSpace(resolution.CustomResolvedValue))
        {
            throw new ArgumentException(
                $"A custom value is required for issue '{issue.IssueId}'.",
                nameof(resolution));
        }

        var previousAction = issue.ResolutionAction;
        var previousCustomValue = issue.CustomResolvedValue;

        issue.ResolutionAction = resolution.ResolutionAction;
        issue.CustomResolvedValue = resolution.ResolutionAction == ImportResolutionAction.UseCustomValue
            ? resolution.CustomResolvedValue
            : null;
        issue.IsResolved = resolution.ResolutionAction != ImportResolutionAction.None;

        report.AuditTrail.Add(new ImportAuditEntry
        {
            Timestamp = timestamp,
            UserId = userId,
            Action = issue.IsResolved ? "IssueResolutionChanged" : "IssueResolutionCleared",
            IssueId = issue.IssueId,
            PreviousResolutionAction = previousAction,
            ResolutionAction = issue.ResolutionAction,
            PreviousCustomResolvedValue = previousCustomValue,
            CustomResolvedValue = issue.CustomResolvedValue
        });
    }

    private static ImportDecision DetermineDecision(IReadOnlyCollection<ImportIssue> issues)
    {
        var hasUnresolvedErrors = issues.Any(issue =>
            issue.Severity >= ImportIssueSeverity.Error && !issue.IsResolved);

        return new ImportDecision
        {
            Type = hasUnresolvedErrors
                ? ImportDecisionType.Abort
                : ImportDecisionType.Continue,
            Reason = hasUnresolvedErrors
                ? "The import contains unresolved errors."
                : "The import contains no unresolved errors."
        };
    }

    private static ImportStatus DetermineStatus(IReadOnlyCollection<ImportIssue> issues)
    {
        var hasBlockingIssues = issues.Any(issue =>
            (issue.RequiresUserDecision && !issue.IsResolved) ||
            (issue.Severity >= ImportIssueSeverity.Error && !issue.IsResolved));

        return hasBlockingIssues
            ? ImportStatus.AwaitingResolution
            : ImportStatus.ReadyToCommit;
    }
}
