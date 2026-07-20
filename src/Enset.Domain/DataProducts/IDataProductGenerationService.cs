public interface IDataProductGenerationService
{
    Task<DataProductVersion> GenerateAsync(
        GenerateDataProductRequest request,
        CancellationToken cancellationToken = default);
}