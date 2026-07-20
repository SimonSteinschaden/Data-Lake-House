public class DataProductScopeAssignment : BaseEntity
{
    public Guid DataProductId { get; set; }
    public DataProduct DataProduct { get; set; } = null!;

    public DataProductScopeType ScopeType { get; set; }

    public Guid? MeterId { get; set; }
    public Meter? Meter { get; set; }

    public Guid? EnergySystemId { get; set; }
    public EnergySystem? EnergySystem { get; set; }

    public Guid? BuildingId { get; set; }
    public Building? Building { get; set; }

    public Guid? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }

    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? EnergyCommunityId { get; set; }
    public EnergyCommunity? EnergyCommunity { get; set; }
}