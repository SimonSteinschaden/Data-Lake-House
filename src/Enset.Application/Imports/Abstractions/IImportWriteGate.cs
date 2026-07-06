using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportWriteGate
{
    bool CanWrite(ImportWriteContext context);
}