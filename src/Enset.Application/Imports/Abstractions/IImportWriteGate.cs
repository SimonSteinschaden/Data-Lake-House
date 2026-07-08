using Enset.Application.Imports.WriteGate;

namespace Enset.Application.Imports.Abstractions;

public interface IImportWriteGate
{
    ImportWriteGateResult Evaluate(ImportWriteContext context);

    bool CanWrite(ImportWriteContext context);
}
