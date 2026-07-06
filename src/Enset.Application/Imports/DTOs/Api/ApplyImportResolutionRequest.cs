using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class ApplyImportResolutionRequest
{
    public Guid ImportId { get; init; }

    public bool UserConfirmed { get; init; }

    public IReadOnlyList<ImportIssueResolution> Resolutions { get; init; } = [];
}
