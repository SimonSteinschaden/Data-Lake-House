using Enset.Domain.Common;
using Enset.Domain.Geography;

namespace Enset.Domain.Buildings;

public class BuildingVersion : BaseEntity
{
    public Guid BuildingId { get; set; }

    public Building Building { get; set; } = null!;

    public int VersionNumber { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public string? ChangeReason { get; set; }

    public Guid? AddressId { get; set; }

    public Address? Address { get; set; }

    public string? CadastralMunicipality { get; set; }

    public string? PropertyNumber { get; set; }

    public string? BuildingRegistryIdentifier { get; set; }

    public PrimaryUseType PrimaryUseType { get; set; }

    public BuildingCategory BuildingCategory { get; set; }

    public OwnershipType OwnershipType { get; set; }

    public bool IsResidential { get; set; }

    public bool IsCommercial { get; set; }

    public bool IsPublic { get; set; }

    public bool HasMixedUse { get; set; }

    public int? YearOfConstruction { get; set; }

    public int? YearOfLastMajorRenovation { get; set; }

    public decimal? GrossFloorAreaM2 { get; set; }

    public decimal? NetFloorAreaM2 { get; set; }

    public decimal? ConditionedFloorAreaM2 { get; set; }

    public decimal? HeatedFloorAreaM2 { get; set; }

    public decimal? CooledFloorAreaM2 { get; set; }

    public decimal? BuildingVolumeM3 { get; set; }

    public int? NumberOfFloors { get; set; }

    public int? NumberOfUsageUnits { get; set; }

    public bool IsProtectedBuilding { get; set; }

    public bool IsTemporaryBuilding { get; set; }
}
