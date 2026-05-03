public interface IImportService
{
    Task ImportMeterReadingsAsync(
        Stream inputStream,
        ImportSourceType sourceType,
        Guid importJobId,
        CancellationToken cancellationToken = default);
}