using Enset.Domain.Common;
using Enset.Domain.Energy;

namespace Enset.Domain.EnergyCommunities;

public class EnergyCommunityMeterAssignment : BaseEntity
{
    public Guid EnergyCommunityId { get; set; }

    public EnergyCommunity EnergyCommunity { get; set; } = null!;

    public Guid MeterId { get; set; }

    public Meter Meter { get; set; } = null!;

    public EnergyCommunityMeterRole Role { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public decimal? AllocationShare { get; set; }

    public bool IsActive { get; set; } = true;
}