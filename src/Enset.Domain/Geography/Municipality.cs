using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class Municipality : BaseEntity
{
    public Guid DistrictId { get; set; }

    public District District { get; set; } = null!;

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<PostalCodeArea> PostalCodeAreas { get; set; }
        = new List<PostalCodeArea>();

    public ICollection<Region> Regions { get; set; } = new List<Region>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
