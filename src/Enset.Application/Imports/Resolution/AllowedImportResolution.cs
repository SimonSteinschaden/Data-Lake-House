using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public sealed class AllowedImportResolution
{
    public ImportResolutionAction Action { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool RequiresInput { get; init; }
    public ResolutionInputType InputType { get; init; }
    public bool SupportsBatch { get; init; }
    public string? Culture { get; init; }
}
