using Enset.Domain.Common;
using Enset.Domain.Projects;

namespace Enset.Domain.Customers;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public CustomerType Type { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

