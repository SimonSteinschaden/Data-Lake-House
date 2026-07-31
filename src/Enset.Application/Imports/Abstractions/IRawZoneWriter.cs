using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IRawZoneWriter
{
    Task<string> ArchiveAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default);
}
