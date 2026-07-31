using Enset.Domain.Common;

namespace Enset.Domain.EnergyCommunities;

public class EnergyCommunity : BaseEntity
{
    public string CommunityNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public EnergyCommunityType Type { get; set; }

    public EnergyCommunityScope Scope { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EnergyCommunityMeterAssignment> MeterAssignments
        { get; set; }
        = new List<EnergyCommunityMeterAssignment>();

    // TODO Phase 5+:
    // Member management
    // Allocation rules
    // Settlement
    // Billing
    // Network operator registration
    // P2P contract details
}