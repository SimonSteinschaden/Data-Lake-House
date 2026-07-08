using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class ApplyImportResolutionRequest
{
    public string UserId { get; init; } = string.Empty;

    public IReadOnlyList<ImportIssueResolution> Resolutions { get; init; } = [];
}
