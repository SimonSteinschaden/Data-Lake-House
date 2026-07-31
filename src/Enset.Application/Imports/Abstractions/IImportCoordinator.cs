using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Abstractions;

public interface IImportCoordinator
{
    Task<ImportReport> RunAsync(CancellationToken cancellationToken = default);
}
