namespace Enset.Application.Imports.WriteGate;

public sealed class ImportWriteGateResult
{
    public bool IsAllowed => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ImportWriteGateResult Allowed { get; } = new();
}
