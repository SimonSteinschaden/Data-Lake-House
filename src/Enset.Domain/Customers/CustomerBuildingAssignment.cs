using Enset.Domain.Buildings;
using Enset.Domain.Common;

namespace Enset.Domain.Customers;

public class CustomerBuildingAssignment : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public Guid BuildingId { get; set; }

    public Building Building { get; set; } = null!;

    public CustomerBuildingRole Role { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsPrimary { get; set; }
}