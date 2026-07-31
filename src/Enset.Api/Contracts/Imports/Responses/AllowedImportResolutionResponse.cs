using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Resolution;

namespace Enset.Api.Contracts.Imports.Responses;

public sealed class AllowedImportResolutionResponse
{
    public ImportResolutionAction Type { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool RequiresInput { get; init; }
    public ResolutionInputType InputType { get; init; }
    public bool SupportsBatch { get; init; }
    public string? Culture { get; init; }
}
