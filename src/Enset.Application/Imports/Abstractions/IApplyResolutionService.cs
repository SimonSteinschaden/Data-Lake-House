using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IApplyResolutionService
{
    ImportWriteContext Apply(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        bool userConfirmed);
}
