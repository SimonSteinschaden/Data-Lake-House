using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class Country : BaseEntity
{
    public string IsoCode2 { get; set; } = string.Empty;

    public string? IsoCode3 { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<State> States { get; set; } = new List<State>();

    public ICollection<PostalCodeArea> PostalCodeAreas { get; set; }
        = new List<PostalCodeArea>();

    public ICollection<Region> Regions { get; set; } = new List<Region>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
