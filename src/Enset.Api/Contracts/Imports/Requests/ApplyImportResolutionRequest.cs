using Enset.Application.Imports.Resolution;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class ApplyImportResolutionRequest
{
    public string UserId { get; init; } = string.Empty;

    public IReadOnlyList<ImportIssueResolution> Resolutions { get; init; } = [];
}
