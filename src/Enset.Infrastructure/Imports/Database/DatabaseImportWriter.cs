using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;

namespace Enset.Infrastructure.Imports.Database;

public sealed class DatabaseImportWriter : IImportWriter
{
    public ImportWriterType WriterType => ImportWriterType.Database;

    public Task WriteAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Database import mapping is not implemented yet. No database data was changed.");
    }
}
