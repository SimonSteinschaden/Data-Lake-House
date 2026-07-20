public class DataProductDefinitionScope : BaseEntity
{
    public Guid DataProductDefinitionId { get; set; }

    public DataProductDefinition DataProductDefinition { get; set; } = null!;

    public DataProductScopeType ScopeType { get; set; }
}