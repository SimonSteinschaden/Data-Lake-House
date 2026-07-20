using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Domain.Data;
using Enset.Domain.DataProducts;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.DataProducts;

public sealed class EfDataProductReader : IMeterReadingDataReader, IBuildingDataReader
{
    private readonly EnsetDbContext _db;
    public EfDataProductReader(EnsetDbContext db) => _db = db;

    public async Task<IReadOnlyList<MeterReadingData>> GetConsumptionReadingsAsync(
        Guid dataProductId, DateTime periodFrom, DateTime periodTo,
        CancellationToken cancellationToken = default)
    {
        var scope = _db.DataProductScopeAssignments.Where(x => x.DataProductId == dataProductId);
        var meterIds = scope.Where(x => x.MeterId != null).Select(x => x.MeterId!.Value)
            .Concat(_db.Meters.Where(m => m.BuildingId != null
                && scope.Any(s => s.BuildingId == m.BuildingId)).Select(m => m.Id));

        return await _db.MeterReadings.AsNoTracking()
            .Where(x => meterIds.Contains(x.MeterId)
                && x.Timestamp >= periodFrom && x.Timestamp < periodTo
                && x.Meter.Direction == MeterDirection.Consumption
                && x.QualityFlag != DataQuality.Invalid && x.QualityFlag != DataQuality.Missing)
            .OrderBy(x => x.MeterId).ThenBy(x => x.Timestamp)
            .Select(x => new MeterReadingData(x.MeterId, x.Timestamp, x.Value,
                x.Meter.Unit.ToString(), x.ReadingType, x.Meter.Quantity,
                x.Meter.Direction, x.QualityFlag, x.IntervalSeconds))
            .ToListAsync(cancellationToken);
    }

    public async Task<BuildingData?> GetAsync(Guid dataProductId,
        DateTime periodFrom, DateTime periodTo,
        CancellationToken cancellationToken = default)
    {
        return await _db.DataProductScopeAssignments.AsNoTracking()
            .Where(x => x.DataProductId == dataProductId && x.BuildingId != null)
            .Select(x => new BuildingData(x.Building!.Id, x.Building.Name,
                x.Building.Meters.Where(m => m.IsActive
                    && (m.CommissionedAt == null || m.CommissionedAt <= periodTo)
                    && (m.DecommissionedAt == null || m.DecommissionedAt >= periodFrom))
                    .Select(m => m.Id).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
