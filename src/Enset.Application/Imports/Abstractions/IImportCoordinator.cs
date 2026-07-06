namespace Enset.Application.Imports.Abstractions;

public interface IImportCoordinator
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
