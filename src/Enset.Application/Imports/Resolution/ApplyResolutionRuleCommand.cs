using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public sealed class ApplyResolutionRuleCommand
{
    public Guid RuleId { get; init; } = Guid.NewGuid();
    public Guid SeedIssueId { get; init; }
    public ResolutionScope Scope { get; init; }
    public ImportResolutionType ResolutionType { get; init; }
    public ImportResolutionAction ResolutionAction { get; init; }
    public string? ResolutionPayload { get; init; }
}
