using Enset.Application.Imports.Abstractions;
using System.Linq;
using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Services;

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
        return (await _db.Meters
            .Where(m => m.MeterNumber != null)
            .ToListAsync<Meter>(cancellationToken))
            .ToDictionary(
                m => m.MeterNumber!,
                m => m.Id);
    }
}