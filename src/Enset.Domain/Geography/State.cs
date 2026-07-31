using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class State : BaseEntity
{
    public Guid CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<District> Districts { get; set; } = new List<District>();
}
