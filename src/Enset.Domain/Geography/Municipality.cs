using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class Municipality : BaseEntity
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? ZipCode { get; set; }

    public ICollection<District> Districts { get; set; } = new List<District>();
}