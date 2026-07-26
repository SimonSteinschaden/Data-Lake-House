
using System.Text.Json.Serialization;
using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.Issues;

public class ImportIssue
{
    public Guid IssueId { get; init; } = Guid.NewGuid();

    public Guid? EntityId { get; init; }

    public ImportIssueType Type { get; init; }

    public ImportIssueSeverity Severity { get; init; }

    public string Message { get; init; } = string.Empty;

    public double? SimilarityScore { get; set; }

    public bool RequiresUserDecision { get; set; }

    // Duplication Resolution

    public string? FieldName { get; set; }

    public int? SourceRowNumber { get; set; }

    public string? FirstValue { get; set; }

    public string? SecondValue { get; set; }

    public ImportIssueValuePattern ValuePattern { get; set; }

    public ResolutionTargetDataType TargetDataType { get; set; }

    public NumberFormatPattern NumberFormatPattern { get; set; }

    public ImportResolutionAction ResolutionAction { get; set; }
        = ImportResolutionAction.None;

    public string? CustomResolvedValue { get; set; }

    public bool IsResolved { get; set; }

    [JsonInclude]
    public ImportResolutionSource ResolutionSource { get; private set; }

    [JsonInclude]
    public DateTime? ResolvedAt { get; private set; }

    [JsonInclude]
    public string? ResolvedBy { get; private set; }

    [JsonInclude]
    public ResolutionScope? ResolutionScope { get; private set; }

    [JsonInclude]
    public Guid? ResolutionRuleId { get; private set; }

    public bool IsCommitBlocking =>
        !IsResolved &&
        (RequiresUserDecision || Severity >= ImportIssueSeverity.Error);

    public void ResolveAutomatically(
        ImportResolutionAction resolutionAction,
        DateTime resolvedAt,
        string? customResolvedValue = null)
    {
        if (resolutionAction == ImportResolutionAction.None)
            throw new ArgumentException("An automatic resolution action is required.", nameof(resolutionAction));

        RequiresUserDecision = false;
        SetResolution(
            resolutionAction,
            customResolvedValue,
            ImportResolutionSource.Automatic,
            resolvedAt,
            resolvedBy: null);
    }

    public void ResolveManually(
        ImportResolutionAction resolutionAction,
        string? customResolvedValue,
        string resolvedBy,
        DateTime resolvedAt,
        ResolutionScope scope =
            Enset.Application.Imports.Resolution.ResolutionScope.SingleIssue,
        Guid? resolutionRuleId = null)
    {
        if (!RequiresUserDecision &&
            !IsCommitBlocking &&
            ImportResolutionOptionsProvider.GetOptions(this).Count == 0)
            throw new InvalidOperationException($"Issue '{IssueId}' does not accept a user resolution.");

        if (string.IsNullOrWhiteSpace(resolvedBy))
            throw new ArgumentException("A resolving user is required.", nameof(resolvedBy));

        if (resolutionAction == ImportResolutionAction.None)
        {
            ClearResolution();
            return;
        }

        SetResolution(
            resolutionAction,
            customResolvedValue,
            ImportResolutionSource.Manual,
            resolvedAt,
            resolvedBy);
        ResolutionScope = scope;
        ResolutionRuleId = resolutionRuleId;
    }

    public void RestoreResolutionMetadata(
        ImportResolutionSource source,
        DateTime? resolvedAt,
        string? resolvedBy)
    {
        ResolutionSource = source;
        ResolvedAt = resolvedAt;
        ResolvedBy = resolvedBy;
    }

    public void ResolveByRule(
        ImportResolutionAction resolutionAction,
        string? resolutionPayload,
        string resolvedBy,
        DateTime resolvedAt,
        ResolutionScope scope,
        Guid resolutionRuleId)
    {
        if (resolutionAction == ImportResolutionAction.None)
            throw new ArgumentException("A resolution action is required.", nameof(resolutionAction));
        if (string.IsNullOrWhiteSpace(resolvedBy))
            throw new ArgumentException("A resolving user is required.", nameof(resolvedBy));

        SetResolution(
            resolutionAction,
            resolutionPayload,
            ImportResolutionSource.Manual,
            resolvedAt,
            resolvedBy);
        ResolutionScope = scope;
        ResolutionRuleId = resolutionRuleId;
    }

    private void SetResolution(
        ImportResolutionAction resolutionAction,
        string? customResolvedValue,
        ImportResolutionSource source,
        DateTime resolvedAt,
        string? resolvedBy)
    {
        ResolutionAction = resolutionAction;
        CustomResolvedValue = customResolvedValue;
        IsResolved = true;
        ResolutionSource = source;
        ResolvedAt = resolvedAt;
        ResolvedBy = resolvedBy;
    }

    private void ClearResolution()
    {
        ResolutionAction = ImportResolutionAction.None;
        CustomResolvedValue = null;
        IsResolved = false;
        ResolutionSource = ImportResolutionSource.None;
        ResolvedAt = null;
        ResolvedBy = null;
        ResolutionScope = null;
        ResolutionRuleId = null;
    }
}
