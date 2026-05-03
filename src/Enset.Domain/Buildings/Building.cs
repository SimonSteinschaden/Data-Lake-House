using Enset.Domain.Common;
using Enset.Domain.Projects;
using Enset.Domain.Geography;
using Enset.Domain.Energy;

namespace Enset.Domain.Buildings;

public class Building : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid DistrictId { get; set; }

    public District District { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public PrimaryUseType PrimaryUseType { get; set; }

    public BuildingCategory BuildingCategory { get; set; }

    public OwnershipType OwnershipType { get; set; }

    public bool IsResidential { get; set; }

    public bool IsCommercial { get; set; }

    public bool IsPublic { get; set; }

    public bool HasMixedUse { get; set; }

    public int? YearOfConstruction { get; set; }

    public decimal? FloorAreaM2 { get; set; }

    public ICollection<EnergySystem> EnergySystems { get; set; } = new List<EnergySystem>();

    public ICollection<Meter> Meters { get; set; } = new List<Meter>();
}