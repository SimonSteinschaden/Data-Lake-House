public interface IMeterReadingMapper
{
    Task<MeterReading> MapAsync(
        MeterReadingImportDto dto,
        CancellationToken cancellationToken = default);
}