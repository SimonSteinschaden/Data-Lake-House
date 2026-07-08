using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;

namespace Enset.Worker;

public sealed class ConsoleImportWriter : IImportWriter
{
    public ImportWriterType WriterType { get; }

    public ConsoleImportWriter(ImportWriterType writerType = ImportWriterType.Excel)
    {
        WriterType = writerType;
    }

    public Task WriteAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(
            $"Import freigegeben: {context.Customers.Count} Kunden, " +
            $"{context.Issues.Count} Hinweise.");

        return Task.CompletedTask;
    }
}
