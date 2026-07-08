using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.Abstractions;

public interface IApplyResolutionService
{
    ImportReport Apply(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        string userId,
        DateTime timestamp);
}
