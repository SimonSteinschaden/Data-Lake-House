using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class PostalCodeArea : BaseEntity
{
    public Guid CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public string? Name { get; set; }

    public ICollection<Municipality> Municipalities { get; set; }
        = new List<Municipality>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
