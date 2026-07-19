using Enset.Domain.Common;
using Enset.Domain.Customers;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.Geography;
using Enset.Domain.Projects;

namespace Enset.Domain.Buildings;

public class Building : BaseEntity
{
    public string BuildingNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? DistrictId { get; set; }

    public District? District { get; set; }

    public string? ExternalIdentifier { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BuildingVersion> Versions { get; set; }
        = new List<BuildingVersion>();

    public ICollection<CustomerBuildingAssignment> CustomerAssignments { get; set; }
        = new List<CustomerBuildingAssignment>();

    public ICollection<EnergySystemBuildingAssignment> EnergySystemAssignments { get; set; }
    = new List<EnergySystemBuildingAssignment>();

    public ICollection<Meter> Meters { get; set; }
        = new List<Meter>();

    public ICollection<Document> Documents { get; set; }
        = new List<Document>();
}