using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class Region : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Municipality> Municipalities { get; set; } = new List<Municipality>();
}