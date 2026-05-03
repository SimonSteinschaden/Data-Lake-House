using Enset.Domain.Common;
using Enset.Domain.Buildings;

namespace Enset.Domain.Geography;

public class District : BaseEntity
{
    public Guid MunicipalityId { get; set; }

    public Municipality Municipality { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}