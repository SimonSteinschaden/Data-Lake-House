using Enset.Domain.Common;
using Enset.Domain.Customers;

namespace Enset.Domain.DataProducts;

public class DataProductCustomerAssignment : BaseEntity
{
    public Guid DataProductId { get; set; }

    public DataProduct DataProduct { get; set; } = null!;

    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public DataProductCustomerRole Role { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
}