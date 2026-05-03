public class MeterLookupService : IMeterLookupService
{
    private readonly EnsetDbContext _db;

    public MeterLookupService(EnsetDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, Guid>> GetMeterLookupAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Meters
            .Where(m => m.MeterNumber != null)
            .ToDictionaryAsync(
                m => m.MeterNumber,
                m => m.Id,
                cancellationToken);
    }
}