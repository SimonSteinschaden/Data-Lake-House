using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportCommitService
{
    Task<ImportCommitResult> CommitAsync(
    ImportCommitCommand command,
    CancellationToken cancellationToken = default);
}
