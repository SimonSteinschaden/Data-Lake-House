using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportWriter
{
    void Write(ImportWriteContext context);
}