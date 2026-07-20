public interface IDataProductGenerator
{
    string DefinitionCode { get; }

    Task<DataProductGenerationResult> GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default);
}