using Enset.Domain.Common;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Definiert Typ, Ergebnisform und zulässige Geltungsbereiche eines Datenprodukts.
/// </summary>
public class DataProductDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DataProductCategory Category { get; set; }

    public DataProductResultType ResultType { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DataProductDefinitionScope> AllowedScopes
        { get; set; } = new List<DataProductDefinitionScope>();

    public ICollection<DataProduct> DataProducts
        { get; set; } = new List<DataProduct>();
}
