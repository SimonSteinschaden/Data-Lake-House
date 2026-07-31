using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportWriter
{
    ImportWriterType WriterType { get; }

    Task WriteAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default);
}
