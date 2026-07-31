namespace Enset.Domain.Curation;

public enum DataMaturityLevel
{
    Bronze,
    Silver,
    Gold
}

public enum CurationTaskStatus
{
    Open,
    Accepted,
    Customized,
    Rejected
}

public enum CurationSource
{
    Import,
    User,
    EnsetSuggestion,
    System
}

public enum BuildingState { Existing, Improved, Planned, Target, Unknown }

public sealed class CurationTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityDisplayName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string SuggestedValue { get; set; } = string.Empty;
    public string SuggestedNormalizedValue { get; set; } = string.Empty;
    public int ConfidencePercent { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = "1.0";
    public string Reasoning { get; set; } = string.Empty;
    public CurationTaskStatus Status { get; set; } = CurationTaskStatus.Open;
    public string? CuratedValue { get; set; }
    public CurationSource Source { get; set; } = CurationSource.EnsetSuggestion;
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public ICollection<CurationDecision> Decisions { get; set; } = new List<CurationDecision>();
}

public sealed class CuratedFieldValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string CuratedValue { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public CurationSource Source { get; set; }
    public DataMaturityLevel MaturityLevel { get; set; }
    public int ConfidencePercent { get; set; }
    public string? RuleId { get; set; }
    public string? RuleVersion { get; set; }
    public bool Confirmed { get; set; }
    public Guid ConfirmedByUserId { get; set; }
    public DateTime ConfirmedAtUtc { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public Guid? ImportId { get; set; }
    public uint RowVersion { get; set; }
}

public sealed class CurationDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CurationTaskId { get; set; }
    public CurationTask Task { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
    public CurationTaskStatus Decision { get; set; }
    public string? OriginalValue { get; set; }
    public string SuggestedValue { get; set; } = string.Empty;
    public string? NewValue { get; set; }
    public CurationSource Source { get; set; }
    public int ConfidencePercent { get; set; }
    public string? Reason { get; set; }
}
