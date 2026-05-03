public class Project : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public ProjectStatus Status { get; set; }

    public ICollection<Building> Buildings { get; set; } = new List<Building>();

    public ICollection<Document> Documents { get; set; } = new List<Document>();

}