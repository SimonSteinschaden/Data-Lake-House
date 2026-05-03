public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public CustomerType Type { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

