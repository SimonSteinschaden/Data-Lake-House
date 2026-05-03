public class MeterReadingMapper : IMeterReadingMapper
{
    private readonly EnsetDbContext _db;

    public MeterReadingMapper(EnsetDbContext db)
    {
        _db = db;
    }

    public async Task<MeterReading> MapAsync(
        MeterReadingImportDto dto,
        CancellationToken cancellationToken = default)
    {
        // 🔹 1. Meter finden
        var meter = await _db.Meters
            .FirstOrDefaultAsync(m => m.MeterNumber == dto.MeterNumber, cancellationToken);

        // 🔹 2. Falls nicht vorhanden → erstellen
        if (meter == null)
        {
            meter = new Meter
            {
                Id = Guid.NewGuid(),
                MeterNumber = dto.MeterNumber,
                PostalCode = dto.PostalCode
            };

            _db.Meters.Add(meter);
        }

        // 🔹 3. MeterReading erzeugen
        var reading = new MeterReading
        {
            MeterId = meter.Id,
            Timestamp = dto.Timestamp,
            Value = dto.Value,
            Unit = dto.Unit ?? "kWh",
            QualityFlag = (DataQuality)(dto.QualityFlag ?? 0),
            BuildingId = dto.BuildingId,
            CustomerId = dto.CustomerId
        };

        return reading;
    }
}