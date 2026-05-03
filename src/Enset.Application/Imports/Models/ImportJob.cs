public class ImportJob : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ImportSourceType SourceType { get; set; }

    public ImportStatus Status { get; set; }

}