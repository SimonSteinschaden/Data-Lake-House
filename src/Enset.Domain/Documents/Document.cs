using Enset.Domain.Common;
using Enset.Domain.Projects;
using Enset.Domain.Associations;

namespace Enset.Domain.Documents;

public class Document : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DocumentType Type { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BuildingDocumentAssignment> BuildingAssignments { get; set; }
        = new List<BuildingDocumentAssignment>();
}
