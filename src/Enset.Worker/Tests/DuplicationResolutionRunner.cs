using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.WriteGate;

namespace Enset.Worker.Tests;

/// <summary>
/// Developer test harness that deliberately uses the same resolution and commit
/// services as API and future UI clients.
/// </summary>
public static class DuplicationResolutionRunner
{
    public static async Task<ImportCommitResult> RunAsync(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        ImportCommitCommand commitRequest,
        IApplyResolutionService resolutionService,
        IImportReportRepository reports,
        IImportCommitService commitService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(commitRequest);

        resolutionService.Apply(
            report,
            resolutions,
            commitRequest.UserId,
            commitRequest.Timestamp);

        await reports.SaveAsync(report, cancellationToken);
        return await commitService.CommitAsync(commitRequest, cancellationToken);
    }
}
