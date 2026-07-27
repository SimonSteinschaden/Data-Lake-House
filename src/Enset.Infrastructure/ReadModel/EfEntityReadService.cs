using Enset.Application.Authorization;
using Enset.Application.ReadModel;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.ReadModel;

public sealed class EfEntityReadService : IEntityReadService
{
    public const int MaximumPageSize = 200;
    private readonly EnsetDbContext _db;
    private readonly IDataAccessScope _scope;

    public EfEntityReadService(EnsetDbContext db, IDataAccessScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(
        CustomerListQuery request, CancellationToken ct = default)
    {
        var (page, size) = Page(request.Page, request.PageSize);
        var now = DateTime.UtcNow;
        var query = _scope.ApplyCustomerScope(_db.Customers.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%") ||
                EF.Functions.ILike(x.CustomerNumber, $"%{search}%"));
        }
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);
        var ordered = request.SortBy.Equals("customerNumber", StringComparison.OrdinalIgnoreCase)
            ? Order(query, request.SortDirection, x => x.CustomerNumber)
            : Order(query, request.SortDirection, x => x.Name);
        var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new CustomerSummaryDto(x.Id, x.CustomerNumber, x.Name,
                x.Type.ToString(), x.IsActive, x.BuildingAssignments.Count(a =>
                    a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now))))
            .ToListAsync(ct);
        return new(items, page, size, total);
    }

    public async Task<CustomerDetailDto?> GetCustomerAsync(Guid id,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _scope.ApplyCustomerScope(_db.Customers.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new CustomerDetailDto(x.Id, x.CustomerNumber, x.Name,
                x.LegalName, x.Type.ToString(), x.Email, x.Phone, x.Website,
                x.Street, x.HouseNumber, x.PostalCode, x.City, x.CountryCode,
                x.IsActive, x.BuildingAssignments
                    .Where(a => a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now))
                    .OrderBy(a => a.Building.Name)
                    .Select(a => new CustomerBuildingDto(a.BuildingId,
                        a.Building.BuildingNumber, a.Building.Name, a.Role.ToString(),
                        a.IsPrimary)).ToList(), x.DataOrigin.ToString(), x.CreatedAt,
                x.CreatedByUserId, x.UpdatedAt, x.UpdatedByUserId, x.IsDeleted, x.RowVersion))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<PagedResult<BuildingSummaryDto>> GetBuildingsAsync(
        BuildingListQuery request, CancellationToken ct = default)
    {
        var (page, size) = Page(request.Page, request.PageSize);
        var now = DateTime.UtcNow;
        var query = _scope.ApplyBuildingScope(_db.Buildings.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%") ||
                EF.Functions.ILike(x.BuildingNumber, $"%{search}%"));
        }
        if (request.CustomerId.HasValue)
            query = query.Where(x => x.CustomerAssignments.Any(a =>
                a.CustomerId == request.CustomerId.Value && a.ValidFrom <= now &&
                (a.ValidTo == null || a.ValidTo > now)));
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);
        var ordered = request.SortBy.Equals("buildingNumber", StringComparison.OrdinalIgnoreCase)
            ? Order(query, request.SortDirection, x => x.BuildingNumber)
            : Order(query, request.SortDirection, x => x.Name);
        var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new BuildingSummaryDto(x.Id, x.BuildingNumber, x.Name,
                x.ExternalIdentifier, x.IsActive, x.Meters.Count,
                x.Meters.SelectMany(m => m.Readings).Min(r => (DateTime?)r.Timestamp),
                x.Meters.SelectMany(m => m.Readings).Max(r => (DateTime?)r.Timestamp)))
            .ToListAsync(ct);
        return new(items, page, size, total);
    }

    public async Task<BuildingDetailDto?> GetBuildingAsync(Guid id,
        CancellationToken ct = default)
    {
        var allowedCustomers = _scope.ApplyCustomerScope(_db.Customers).Select(x => x.Id);
        var now = DateTime.UtcNow;
        return await _scope.ApplyBuildingScope(_db.Buildings.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new BuildingDetailDto(x.Id, x.BuildingNumber, x.Name,
                x.ExternalIdentifier, x.IsActive, x.Meters.Count,
                x.Meters.SelectMany(m => m.Readings).Min(r => (DateTime?)r.Timestamp),
                x.Meters.SelectMany(m => m.Readings).Max(r => (DateTime?)r.Timestamp),
                x.CustomerAssignments.Where(a => allowedCustomers.Contains(a.CustomerId) &&
                    a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now))
                    .OrderBy(a => a.Customer.Name)
                    .Select(a => new BuildingCustomerDto(a.CustomerId,
                        a.Customer.CustomerNumber, a.Customer.Name, a.Role.ToString(),
                        a.IsPrimary)).ToList(),
                x.Meters.OrderBy(m => m.MeterNumber)
                    .Select(m => new BuildingMeterDto(m.Id, m.MeterNumber, m.Name,
                        m.Unit.ToString(), m.Quantity.ToString(), m.IsActive)).ToList(),
                x.DataOrigin.ToString(), x.CreatedAt, x.CreatedByUserId, x.UpdatedAt,
                x.UpdatedByUserId, x.IsDeleted, x.RowVersion,
                x.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.GrossFloorAreaM2).FirstOrDefault(),
                x.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.YearOfConstruction).FirstOrDefault(),
                x.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.Latitude).FirstOrDefault(),
                x.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.Longitude).FirstOrDefault()))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<PagedResult<MeterSummaryDto>> GetMetersAsync(
        MeterListQuery request, CancellationToken ct = default)
    {
        var (page, size) = Page(request.Page, request.PageSize);
        var now = DateTime.UtcNow;
        var query = _scope.ApplyMeterScope(_db.Meters.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%") ||
                EF.Functions.ILike(x.MeterNumber, $"%{search}%"));
        }
        if (request.CustomerId.HasValue)
            query = query.Where(x => x.Building != null &&
                x.Building.CustomerAssignments.Any(a => a.CustomerId == request.CustomerId &&
                    a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now)));
        if (request.BuildingId.HasValue)
            query = query.Where(x => x.BuildingId == request.BuildingId);
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);
        var ordered = request.SortBy.Equals("name", StringComparison.OrdinalIgnoreCase)
            ? Order(query, request.SortDirection, x => x.Name)
            : Order(query, request.SortDirection, x => x.MeterNumber);
        var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new MeterSummaryDto(x.Id, x.MeterNumber, x.Name,
                x.Unit.ToString(), x.Quantity.ToString(), x.Direction.ToString(),
                x.Type.ToString(), x.IsActive, x.BuildingId,
                x.Building == null ? null : x.Building.Name, x.Readings.LongCount(),
                x.Readings.Min(r => (DateTime?)r.Timestamp),
                x.Readings.Max(r => (DateTime?)r.Timestamp)))
            .ToListAsync(ct);
        return new(items, page, size, total);
    }

    public async Task<MeterDetailDto?> GetMeterAsync(Guid id,
        CancellationToken ct = default)
    {
        return await _scope.ApplyMeterScope(_db.Meters.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new MeterDetailDto(x.Id, x.MeterNumber, x.Name,
                x.Description, x.ExternalIdentifier, x.Medium.ToString(),
                x.Quantity.ToString(), x.Unit.ToString(), x.Direction.ToString(),
                x.Type.ToString(), x.Manufacturer, x.Model, x.SerialNumber,
                x.BuildingId, x.Building == null ? null : x.Building.Name, x.IsActive,
                x.Readings.LongCount(), x.Readings.Min(r => (DateTime?)r.Timestamp),
                x.Readings.Max(r => (DateTime?)r.Timestamp),
                x.Readings.OrderByDescending(r => r.Timestamp)
                    .Select(r => (DateTime?)r.Timestamp).FirstOrDefault(),
                x.Readings.OrderByDescending(r => r.Timestamp)
                    .Select(r => (decimal?)r.Value).FirstOrDefault(),
                x.Readings.OrderByDescending(r => r.Timestamp)
                    .Select(r => r.QualityFlag.ToString()).FirstOrDefault(),
                x.DataOrigin.ToString(), x.CreatedAt, x.CreatedByUserId, x.UpdatedAt,
                x.UpdatedByUserId, x.IsDeleted, x.RowVersion))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<MeterReadingsDto?> GetMeterReadingsAsync(Guid meterId,
        MeterReadingQuery request, CancellationToken ct = default)
    {
        var meter = await _scope.ApplyMeterScope(_db.Meters.AsNoTracking())
            .Where(x => x.Id == meterId)
            .Select(x => new { x.Id, x.MeterNumber, x.Unit, x.Quantity })
            .SingleOrDefaultAsync(ct);
        if (meter is null)
            return null;

        var from = Utc(request.From);
        var to = Utc(request.To);
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
            throw new ArgumentException("'from' must be earlier than 'to'.");

        var scoped = _scope.ApplyMeterReadingScope(_db.MeterReadings.AsNoTracking())
            .Where(x => x.MeterId == meterId);
        var availableFrom = await scoped.MinAsync(x => (DateTime?)x.Timestamp, ct);
        var availableTo = await scoped.MaxAsync(x => (DateTime?)x.Timestamp, ct);
        if (from.HasValue) scoped = scoped.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue) scoped = scoped.Where(x => x.Timestamp < to.Value);

        var readingTypes = await scoped.Select(x => x.ReadingType).Distinct().ToListAsync(ct);
        var typeName = readingTypes.Count switch
        {
            0 => MeterReadingType.Unknown.ToString(),
            1 => readingTypes[0].ToString(),
            _ => "Mixed"
        };

        if (request.Aggregation == MeterReadingAggregation.Raw)
        {
            var (page, size) = Page(request.Page, request.PageSize);
            var total = await scoped.CountAsync(ct);
            var ordered = Desc(request.SortDirection)
                ? scoped.OrderByDescending(x => x.Timestamp)
                : scoped.OrderBy(x => x.Timestamp);
            var values = await ordered.Skip((page - 1) * size).Take(size)
                .Select(x => new RawMeterReadingDto(x.Timestamp, x.Value,
                    x.QualityFlag.ToString(), x.IntervalSeconds)).ToListAsync(ct);
            return new(meter.Id, meter.MeterNumber, meter.Unit.ToString(),
                meter.Quantity.ToString(), typeName, request.Aggregation.ToString(),
                availableFrom, availableTo, from, to,
                new(values, page, size, total), null);
        }

        var aggregated = await AggregateAsync(scoped, request.Aggregation,
            Desc(request.SortDirection), ct);
        return new(meter.Id, meter.MeterNumber, meter.Unit.ToString(),
            meter.Quantity.ToString(), typeName, request.Aggregation.ToString(),
            availableFrom, availableTo, from, to, null, aggregated);
    }

    private static async Task<IReadOnlyList<AggregatedMeterReadingDto>> AggregateAsync(
        IQueryable<MeterReading> source, MeterReadingAggregation aggregation,
        bool descending, CancellationToken ct)
    {
        var grouped = aggregation switch
        {
            MeterReadingAggregation.FifteenMinutes => source.GroupBy(x => new
                { x.Timestamp.Year, x.Timestamp.Month, x.Timestamp.Day, x.Timestamp.Hour,
                  Minute = x.Timestamp.Minute / 15, x.ReadingType }),
            MeterReadingAggregation.Hour => source.GroupBy(x => new
                { x.Timestamp.Year, x.Timestamp.Month, x.Timestamp.Day, x.Timestamp.Hour,
                  Minute = 0, x.ReadingType }),
            MeterReadingAggregation.Day => source.GroupBy(x => new
                { x.Timestamp.Year, x.Timestamp.Month, x.Timestamp.Day, Hour = 0,
                  Minute = 0, x.ReadingType }),
            MeterReadingAggregation.Month => source.GroupBy(x => new
                { x.Timestamp.Year, x.Timestamp.Month, Day = 1, Hour = 0,
                  Minute = 0, x.ReadingType }),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation))
        };

        var rows = await grouped.Select(g => new
        {
            g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, g.Key.Minute,
            g.Key.ReadingType,
            Minimum = g.Min(x => x.Value), Maximum = g.Max(x => x.Value),
            Average = g.Average(x => x.Value),
            Sum = g.Key.ReadingType == MeterReadingType.IntervalValue
                ? (decimal?)g.Sum(x => x.Value) : null,
            First = g.OrderBy(x => x.Timestamp).Select(x => x.Value).First(),
            Last = g.OrderByDescending(x => x.Timestamp).Select(x => x.Value).First(),
            Count = g.Count()
        }).ToListAsync(ct);

        var result = rows.Select(x => new AggregatedMeterReadingDto(
            new DateTime(x.Year, x.Month, x.Day, x.Hour,
                x.Minute * (aggregation == MeterReadingAggregation.FifteenMinutes ? 15 : 1),
                0, DateTimeKind.Utc), x.ReadingType.ToString(), x.Minimum, x.Maximum,
            x.Average, x.Sum, x.First, x.Last, x.Last - x.First, x.Count));
        return (descending ? result.OrderByDescending(x => x.BucketStart)
            : result.OrderBy(x => x.BucketStart)).ToList();
    }

    private static (int Page, int Size) Page(int page, int size) =>
        (Math.Max(1, page), Math.Clamp(size, 1, MaximumPageSize));
    private static bool Desc(string value) =>
        value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    private static IOrderedQueryable<T> Order<T, TKey>(IQueryable<T> query,
        string direction, System.Linq.Expressions.Expression<Func<T, TKey>> key) =>
        Desc(direction) ? query.OrderByDescending(key) : query.OrderBy(key);
    private static DateTime? Utc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        var unspecified => DateTime.SpecifyKind(unspecified.Value, DateTimeKind.Utc)
    };
}
