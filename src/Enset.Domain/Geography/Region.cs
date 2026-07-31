using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class Region : BaseEntity
{
    public Guid? CountryId { get; set; }

    public Country? Country { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Municipality> Municipalities { get; set; }
        = new List<Municipality>();
}
