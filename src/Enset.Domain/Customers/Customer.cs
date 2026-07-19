using Enset.Domain.Common;
using Enset.Domain.Projects;
using Enset.Domain.Buildings;

namespace Enset.Domain.Customers;

public class Customer : BaseEntity
{
    public string CustomerNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public CustomerType Type { get; set; }

    public string? CompanyRegistrationNumber { get; set; }

    public string? VatIdentificationNumber { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Street { get; set; }

    public string? HouseNumber { get; set; }

    public string? PostalCode { get; set; }

    public string? City { get; set; }

    public string CountryCode { get; set; } = "AT";

    public bool IsActive { get; set; } = true;

    public ICollection<Project> Projects { get; set; }
        = new List<Project>();

    public ICollection<CustomerBuildingAssignment> BuildingAssignments { get; set; }
        = new List<CustomerBuildingAssignment>();
}