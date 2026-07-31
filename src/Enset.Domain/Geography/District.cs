using Enset.Domain.Common;

namespace Enset.Domain.Geography;

public class District : BaseEntity
{
    public Guid StateId { get; set; }

    public State State { get; set; } = null!;

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Municipality> Municipalities { get; set; }
        = new List<Municipality>();
}
