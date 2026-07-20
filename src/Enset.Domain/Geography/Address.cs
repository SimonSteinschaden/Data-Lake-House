using Enset.Domain.Buildings;
using Enset.Domain.Common;
using Enset.Domain.Energy;

namespace Enset.Domain.Geography;

public class Address : BaseEntity
{
    public Guid CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public Guid? MunicipalityId { get; set; }

    public Municipality? Municipality { get; set; }

    public Guid? PostalCodeAreaId { get; set; }

    public PostalCodeArea? PostalCodeArea { get; set; }

    public string? Street { get; set; }

    public string? HouseNumber { get; set; }

    public string? AddressAddition { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public ICollection<BuildingVersion> BuildingVersions { get; set; }
        = new List<BuildingVersion>();

    public ICollection<EnergySystem> EnergySystems { get; set; }
        = new List<EnergySystem>();
}
