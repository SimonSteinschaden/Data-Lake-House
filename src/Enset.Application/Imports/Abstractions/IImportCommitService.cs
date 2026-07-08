using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportCommitService
{
    Task<ImportCommitResult> CommitAsync(
        ImportCommitRequest request,
        CancellationToken cancellationToken = default);
}
