namespace Enset.Application.Imports.Decisions;

public sealed class ImportDecision
{
    public ImportDecisionType Type { get; init; }

    public string Reason { get; init; } = string.Empty;
}
