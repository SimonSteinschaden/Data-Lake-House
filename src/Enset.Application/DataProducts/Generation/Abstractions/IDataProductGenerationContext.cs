public interface IDataProductGenerator
{
    string DefinitionCode { get; }

    Task<DataProductGenerationAvailability> CheckInputAvailabilityAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default);

    Task GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default);
}