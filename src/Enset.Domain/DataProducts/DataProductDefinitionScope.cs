using Enset.Domain.Common;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Legt einen zulässigen Geltungsbereich für eine Datenproduktdefinition fest.
/// </summary>
public class DataProductDefinitionScope : BaseEntity
{
    public Guid DataProductDefinitionId { get; set; }

    public DataProductDefinition DataProductDefinition { get; set; } = null!;

    public DataProductScopeType ScopeType { get; set; }
}
