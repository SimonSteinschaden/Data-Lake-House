using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.WriteGate;

public sealed class ImportCommitResult
{
    public bool Succeeded { get; init; }

    public ImportReport? Report { get; init; }

    public ImportWriteGateResult GateResult { get; init; } = ImportWriteGateResult.Allowed;
}
