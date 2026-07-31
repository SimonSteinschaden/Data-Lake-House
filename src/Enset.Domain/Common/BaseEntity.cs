namespace Enset.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DataOrigin DataOrigin { get; set; } = DataOrigin.ManuallyCreated;
    public Guid? LastImportId { get; set; }
    public LastModifiedSource LastModifiedSource { get; set; } = LastModifiedSource.User;
    public uint RowVersion { get; set; }
}
