using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Resolution;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class ApplyResolutionRuleRequest
{
    public Guid RuleId { get; init; } = Guid.NewGuid();
    public Guid SeedIssueId { get; init; }
    public ResolutionScope Scope { get; init; }
    public ImportResolutionType ResolutionType { get; init; }
    public ImportResolutionAction ResolutionAction { get; init; }
    public string? ResolutionPayload { get; init; }
}
