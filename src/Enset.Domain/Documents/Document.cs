using Enset.Domain.Common;
using Enset.Domain.Projects;

namespace Enset.Domain.Documents;

public class Document : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DocumentType Type { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}