using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.WriteGate;

namespace Enset.Worker;

public sealed class ConsoleImportWriter : IImportWriter
{
    public void Write(ImportWriteContext context)
    {
        Console.WriteLine(
            $"Import freigegeben: {context.Customers.Count} Kunden, " +
            $"{context.Issues.Count} Hinweise.");
    }
}
