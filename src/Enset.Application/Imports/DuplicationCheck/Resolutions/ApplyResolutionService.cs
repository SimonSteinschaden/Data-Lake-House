using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Resolution;

public sealed class ApplyResolutionService : IApplyResolutionService
{
    public ImportWriteContext Apply(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        bool userConfirmed)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(resolutions);

        EnsureUniqueResolutions(resolutions);

        var issuesById = report.Issues.ToDictionary(issue => issue.IssueId);

        foreach (var resolution in resolutions)
        {
            if (!issuesById.TryGetValue(resolution.IssueId, out var issue))
            {
                throw new ArgumentException(
                    $"Issue '{resolution.IssueId}' does not belong to import '{report.ImportId}'.",
                    nameof(resolutions));
            }

            ApplyResolution(issue, resolution);
        }

        return new ImportWriteContext
        {
            Decision = DetermineWriteDecision(report.Issues),
            UserConfirmed = userConfirmed,
            Issues = report.Issues,
            Customers = report.Customers
        };
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
        ImportIssue issue,
        ImportIssueResolution resolution)
    {
        if (!issue.RequiresUserDecision)
        {
            throw new InvalidOperationException(
                $"Issue '{issue.IssueId}' does not accept a user resolution.");
        }

        if (resolution.ResolutionAction == ImportResolutionAction.None)
        {
            throw new ArgumentException(
                $"A resolution action is required for issue '{issue.IssueId}'.",
                nameof(resolution));
        }

        if (resolution.ResolutionAction == ImportResolutionAction.UseCustomValue &&
            string.IsNullOrWhiteSpace(resolution.CustomResolvedValue))
        {
            throw new ArgumentException(
                $"A custom value is required for issue '{issue.IssueId}'.",
                nameof(resolution));
        }

        issue.ResolutionAction = resolution.ResolutionAction;
        issue.CustomResolvedValue = resolution.ResolutionAction == ImportResolutionAction.UseCustomValue
            ? resolution.CustomResolvedValue
            : null;
        issue.IsResolved = true;
    }

    private static ImportDecisionType DetermineWriteDecision(
        IReadOnlyCollection<ImportIssue> issues)
    {
        return issues.Any(issue =>
                issue.Severity >= ImportIssueSeverity.Error &&
                !issue.IsResolved)
            ? ImportDecisionType.Abort
            : ImportDecisionType.Continue;
    }
}
