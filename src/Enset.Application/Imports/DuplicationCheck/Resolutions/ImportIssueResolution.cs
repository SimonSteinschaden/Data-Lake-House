using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public sealed class ImportIssueResolution
{
    public Guid IssueId { get; init; }

    public ImportResolutionAction ResolutionAction { get; init; }

    public string? CustomResolvedValue { get; init; }
}
