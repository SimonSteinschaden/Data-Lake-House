using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Reports;

namespace Enset.Worker;

/// <summary>
/// Runs the import process by coordinating the import coordinator.
/// </summary>
public sealed class ImportRunner
{
    private readonly IImportCoordinator _coordinator;

    public ImportRunner(IImportCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public Task<ImportReport> RunAsync(CancellationToken cancellationToken = default)
    {
        return _coordinator.RunAsync(cancellationToken);
    }
}
