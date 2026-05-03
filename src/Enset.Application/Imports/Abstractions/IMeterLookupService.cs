public interface IMeterLookupService
{
    Task<Dictionary<string, Guid>> GetMeterLookupAsync(
        CancellationToken cancellationToken = default);
}

var meterLookup = await _meterLookupService.GetMeterLookupAsync();

foreach (var dto in dtos)
{
    if (!meterLookup.TryGetValue(dto.MeterNumber, out var meterId))
    {
        // neuen Meter erstellen
        // Id in Cache ergänzen
    }

    // MeterReading mit meterId erzeugen
}