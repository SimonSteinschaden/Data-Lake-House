namespace Enset.Domain.Common;

public enum DataOrigin
{
    Imported,
    ManuallyCreated,
    ImportedAndModified,
    SystemGenerated
}

public enum LastModifiedSource
{
    Import,
    User,
    System
}

public enum EntityChangeType
{
    Created,
    Updated,
    SoftDeleted,
    Restored
}

public sealed class EntityAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public EntityChangeType ChangeType { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public LastModifiedSource Source { get; set; }
    public Guid? ImportId { get; set; }
    public string? Reason { get; set; }
}
