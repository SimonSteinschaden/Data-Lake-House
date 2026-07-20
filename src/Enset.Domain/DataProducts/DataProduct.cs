using Enset.Domain.Common;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Repräsentiert eine konkrete, kundenspezifische Instanz eines Datenprodukts.
/// </summary>
public class DataProduct : BaseEntity
{
    public string ProductNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DefinitionId { get; set; }

    public DataProductDefinition Definition { get; set; } = null!;

    public DataProductStatus Status { get; set; }

    public ICollection<DataProductCustomerAssignment> CustomerAssignments
        { get; set; } = new List<DataProductCustomerAssignment>();

    public ICollection<DataProductVersion> Versions
        { get; set; } = new List<DataProductVersion>();
}

/*DataProductDefinition
→ beschreibt den generellen Produkttyp

DataProduct
→ beschreibt die konkrete, kundenspezifische Produktinstanz*/
