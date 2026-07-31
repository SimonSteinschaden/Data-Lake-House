using Enset.Application.Authorization;
using Enset.Application.CanonicalSnapshots;
using Enset.Application.ReadModel;
using Enset.Domain.Energy;
using Enset.Infrastructure.CanonicalSnapshots;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.ReadModel;

public sealed class EfEntityReadService(
    EnsetDbContext db,
    IDataAccessScope scope,
    ICanonicalSnapshotReader snapshots) : IEntityReadService
{
    public const int MaximumPageSize = 200;

    public EfEntityReadService(EnsetDbContext db, IDataAccessScope scope)
        : this(
            db,
            scope,
            new EfCanonicalSnapshotReader(
                db,
                scope,
                TimeProvider.System))
    {
    }

    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(
        CustomerListQuery request,
        CancellationToken ct = default)
    {
        var portfolio = await snapshots.GetPortfolio(ct);
        var buildingsByCustomer = portfolio.Buildings
            .Where(x => x.CustomerId.HasValue)
            .GroupBy(x => x.CustomerId!.Value)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var metersByBuilding = portfolio.Meters
            .Where(x => x.BuildingId.HasValue)
            .GroupBy(x => x.BuildingId!.Value)
            .ToDictionary(x => x.Key, x => x.Count());
        var values = portfolio.Customers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            values = values.Where(x =>
                Contains(x.Name, search) ||
                Contains(x.CustomerNumber, search));
        }
        if (request.IsActive.HasValue)
            values = values.Where(x =>
                x.IsActive == request.IsActive.Value);
        values = request.SortBy.Equals(
                "customerNumber",
                StringComparison.OrdinalIgnoreCase)
            ? Sort(
                values,
                request.SortDirection,
                x => x.CustomerNumber)
            : Sort(values, request.SortDirection, x => x.Name);
        var projected = values.Select(customer =>
        {
            var buildings = buildingsByCustomer.GetValueOrDefault(
                customer.CustomerId,
                []);
            return new CustomerSummaryDto(
                customer.CustomerId,
                customer.CustomerNumber,
                customer.Name,
                customer.PostalCode,
                customer.City,
                customer.Phone,
                customer.Email,
                customer.IsActive,
                false,
                buildings.Length)
            {
                QualityLevel = customer.Quality.Level.ToString(),
                MeterCount = buildings.Sum(x =>
                    metersByBuilding.GetValueOrDefault(
                        x.BuildingId,
                        0)),
                MunicipalityId = customer.MunicipalityId,
                Municipality = customer.MunicipalityName
            };
        });
        return Page(projected, request.Page, request.PageSize);
    }

    public async Task<CustomerDetailDto?> GetCustomerAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var customer = await snapshots.GetCustomer(id, ct);
        if (customer is null)
            return null;
        var metadata = await scope.ApplyCustomerScope(
                includeDeleted
                    ? db.Customers.IgnoreQueryFilters().AsNoTracking()
                    : db.Customers.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.LegalName,
                Type = x.Type.ToString(),
                x.Website,
                x.Street,
                x.HouseNumber,
                x.CountryCode,
                DataOrigin = x.DataOrigin.ToString(),
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId,
                x.IsDeleted,
                x.RowVersion
            })
            .SingleOrDefaultAsync(ct);
        if (metadata is null)
            return null;
        var portfolio = await snapshots.GetPortfolio(ct);
        var buildings = portfolio.Buildings
            .Where(x => x.CustomerId == id)
            .OrderBy(x => x.Name)
            .ToArray();
        var buildingIds = buildings
            .Select(x => x.BuildingId)
            .ToHashSet();
        var meters = portfolio.Meters
            .Where(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value))
            .ToArray();
        return new CustomerDetailDto(
            customer.CustomerId,
            customer.CustomerNumber,
            customer.Name,
            metadata.LegalName,
            metadata.Type,
            customer.Email,
            customer.Phone,
            customer.ContactPerson,
            metadata.Website,
            metadata.Street,
            metadata.HouseNumber,
            customer.PostalCode,
            customer.City,
            metadata.CountryCode,
            customer.IsActive,
            buildings.Select(x => new CustomerBuildingDto(
                x.BuildingId,
                x.BuildingNumber,
                x.Name,
                "Unknown",
                false,
                x.UsageType,
                meters.Count(m => m.BuildingId == x.BuildingId),
                x.Quality.Level.ToString())).ToArray(),
            metadata.DataOrigin,
            metadata.CreatedAt,
            metadata.CreatedByUserId,
            metadata.UpdatedAt,
            metadata.UpdatedByUserId,
            metadata.IsDeleted,
            metadata.RowVersion,
            meters.Length,
            portfolio.EnergySystems.Count(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value)))
        {
            QualityLevel = customer.Quality.Level.ToString(),
            MunicipalityId = customer.MunicipalityId,
            Municipality = customer.MunicipalityName
        };
    }

    public async Task<PagedResult<BuildingSummaryDto>> GetBuildingsAsync(
        BuildingListQuery request,
        CancellationToken ct = default)
    {
        var portfolio = await snapshots.GetPortfolio(ct);
        var values = portfolio.Buildings.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            values = values.Where(x =>
                Contains(x.Name, search) ||
                Contains(x.BuildingNumber, search));
        }
        if (request.CustomerId.HasValue)
            values = values.Where(x =>
                x.CustomerId == request.CustomerId.Value);
        if (request.IsActive.HasValue)
            values = values.Where(x =>
                x.IsActive == request.IsActive.Value);
        values = request.SortBy.Equals(
                "buildingNumber",
                StringComparison.OrdinalIgnoreCase)
            ? Sort(
                values,
                request.SortDirection,
                x => x.BuildingNumber)
            : Sort(values, request.SortDirection, x => x.Name);
        var projected = values.Select(building =>
            new BuildingSummaryDto(
                building.BuildingId,
                building.BuildingNumber,
                building.Name,
                building.BuildingType,
                building.UsageType,
                building.CustomerNumber,
                building.CustomerName,
                portfolio.Meters.Count(x =>
                    x.BuildingId == building.BuildingId),
                building.BuildingState,
                building.GoldAssessment.MaturityLevel.ToString(),
                building.GoldAssessment.GoldCompletenessPercentage,
                false)
            {
                QualityLevel = building.GoldAssessment.MaturityLevel.ToString(),
                PostalCode = building.PostalCode,
                City = building.City,
                MunicipalityId = building.MunicipalityId,
                Municipality = building.MunicipalityName,
                ConditionedFloorArea = building.ConditionedFloorArea,
                ConstructionYear = building.ConstructionYear
            });
        return Page(projected, request.Page, request.PageSize);
    }

    public async Task<BuildingDetailDto?> GetBuildingAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var building = await snapshots.GetBuilding(id, ct);
        if (building is null)
            return null;
        var metadata = await scope.ApplyBuildingScope(
                includeDeleted
                    ? db.Buildings.IgnoreQueryFilters().AsNoTracking()
                    : db.Buildings.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.ExternalIdentifier,
                DataOrigin = x.DataOrigin.ToString(),
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId,
                x.IsDeleted,
                x.RowVersion
            })
            .SingleOrDefaultAsync(ct);
        if (metadata is null)
            return null;
        var portfolio = await snapshots.GetPortfolio(ct);
        var meters = portfolio.Meters
            .Where(x => x.BuildingId == id)
            .OrderBy(x => x.MeterNumber)
            .ToArray();
        var periods = meters
            .Select(x => x.Readings)
            .ToArray();
        var customers = building.CustomerId.HasValue
            ? new[]
            {
                new BuildingCustomerDto(
                    building.CustomerId.Value,
                    building.CustomerNumber ?? string.Empty,
                    building.CustomerName ?? string.Empty,
                    "Unknown",
                    false)
            }
            : [];
        return new BuildingDetailDto(
            building.BuildingId,
            building.BuildingNumber,
            building.Name,
            metadata.ExternalIdentifier,
            building.IsActive,
            meters.Length,
            periods.Length == 0
                ? null
                : periods.Min(x => x.PeriodStart),
            periods.Length == 0
                ? null
                : periods.Max(x => x.PeriodEnd),
            customers,
            meters.Select(x => new BuildingMeterDto(
                x.MeterId,
                x.MeterNumber,
                x.Name,
                x.Medium?.ToString() ?? "Unknown",
                x.Direction?.ToString() ?? "Unknown",
                x.Unit?.ToString() ?? "Unknown",
                x.Quality.Level.ToString(),
                x.IsActive)).ToArray(),
            metadata.DataOrigin,
            metadata.CreatedAt,
            metadata.CreatedByUserId,
            metadata.UpdatedAt,
            metadata.UpdatedByUserId,
            metadata.IsDeleted,
            metadata.RowVersion,
            building.GrossFloorArea,
            building.ConstructionYear,
            building.BuildingType,
            building.UsageType,
            building.HeatedArea,
            building.RenovationYear,
            building.BuildingState,
            building.PostalCode,
            building.City,
            building.Street,
            building.HouseNumber)
        {
            QualityLevel = building.GoldAssessment.MaturityLevel.ToString(),
            MunicipalityId = building.MunicipalityId,
            Municipality = building.MunicipalityName,
            ConditionedFloorArea = building.ConditionedFloorArea
        };
    }

    public async Task<PagedResult<MeterSummaryDto>> GetMetersAsync(
        MeterListQuery request,
        CancellationToken ct = default)
    {
        var portfolio = await snapshots.GetPortfolio(ct);
        var values = portfolio.Meters.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            values = values.Where(x =>
                Contains(x.Name, search) ||
                Contains(x.MeterNumber, search));
        }
        if (request.CustomerId.HasValue)
            values = values.Where(x =>
                x.CustomerId == request.CustomerId.Value);
        if (request.BuildingId.HasValue)
            values = values.Where(x =>
                x.BuildingId == request.BuildingId.Value);
        if (request.IsActive.HasValue)
            values = values.Where(x =>
                x.IsActive == request.IsActive.Value);
        values = request.SortBy.Equals(
                "name",
                StringComparison.OrdinalIgnoreCase)
            ? Sort(values, request.SortDirection, x => x.Name)
            : Sort(
                values,
                request.SortDirection,
                x => x.MeterNumber);
        var projected = values.Select(ToMeterSummary);
        return Page(projected, request.Page, request.PageSize);
    }

    public async Task<MeterDetailDto?> GetMeterAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var meter = await snapshots.GetMeter(id, ct);
        if (meter is null)
            return null;
        var metadata = await scope.ApplyMeterScope(
                includeDeleted
                    ? db.Meters.IgnoreQueryFilters().AsNoTracking()
                    : db.Meters.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Description,
                x.ExternalIdentifier,
                Type = x.Type.ToString(),
                x.Manufacturer,
                x.Model,
                x.SerialNumber,
                DataOrigin = x.DataOrigin.ToString(),
                x.CreatedAt,
                x.CreatedByUserId,
                x.UpdatedAt,
                x.UpdatedByUserId,
                x.IsDeleted,
                x.RowVersion
            })
            .SingleOrDefaultAsync(ct);
        if (metadata is null)
            return null;
        var latest = await scope.ApplyMeterReadingScope(
                db.MeterReadings.AsNoTracking())
            .Where(x => x.MeterId == id)
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new
            {
                x.Timestamp,
                x.Value,
                Quality = x.QualityFlag.ToString()
            })
            .FirstOrDefaultAsync(ct);
        var readings = meter.Readings;
        return new MeterDetailDto(
            meter.MeterId,
            meter.MeterNumber,
            meter.Name,
            metadata.Description,
            metadata.ExternalIdentifier,
            meter.Medium?.ToString() ?? "Unknown",
            meter.Quantity?.ToString() ?? "Unknown",
            meter.Unit?.ToString() ?? "Unknown",
            meter.Direction?.ToString() ?? "Unknown",
            metadata.Type,
            metadata.Manufacturer,
            metadata.Model,
            metadata.SerialNumber,
            meter.BuildingId,
            meter.BuildingName,
            meter.IsActive,
            readings.MeasurementCount,
            readings.PeriodStart,
            readings.PeriodEnd,
            latest?.Timestamp,
            latest?.Value,
            latest?.Quality,
            metadata.DataOrigin,
            metadata.CreatedAt,
            metadata.CreatedByUserId,
            metadata.UpdatedAt,
            metadata.UpdatedByUserId,
            metadata.IsDeleted,
            metadata.RowVersion,
            meter.BuildingNumber,
            meter.CustomerNumber,
            meter.CustomerName,
            readings.AnnualValue,
            readings.AnnualValueStatus.ToString(),
            readings.IntervalSeconds)
        {
            QualityLevel = meter.Quality.Level.ToString(),
            ReadingType = readings.ReadingType?.ToString(),
            AnnualValueStatus = readings.AnnualValueStatus.ToString(),
            AnnualValueUnit = readings.Unit?.ToString(),
            AnnualValueReferenceYear =
                readings.AnnualValueReferenceYear
        };
    }

    public async Task<MeterReadingsDto?> GetMeterReadingsAsync(
        Guid meterId,
        MeterReadingQuery request,
        CancellationToken ct = default)
    {
        var meter = await snapshots.GetMeter(meterId, ct);
        if (meter is null)
            return null;
        var from = Utc(request.From);
        var to = Utc(request.To);
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
            throw new ArgumentException(
                "'from' must be earlier than 'to'.");
        var scoped = scope.ApplyMeterReadingScope(
                db.MeterReadings.AsNoTracking())
            .Where(x => x.MeterId == meterId);
        if (from.HasValue)
            scoped = scoped.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue)
            scoped = scoped.Where(x => x.Timestamp < to.Value);
        if (request.Aggregation == MeterReadingAggregation.Raw)
        {
            var (page, size) = NormalizePage(
                request.Page,
                request.PageSize);
            var total = await scoped.CountAsync(ct);
            var ordered = Desc(request.SortDirection)
                ? scoped.OrderByDescending(x => x.Timestamp)
                : scoped.OrderBy(x => x.Timestamp);
            var values = await ordered
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new RawMeterReadingDto(
                    x.Timestamp,
                    x.Value,
                    x.QualityFlag.ToString(),
                    x.IntervalSeconds))
                .ToListAsync(ct);
            return new(
                meter.MeterId,
                meter.MeterNumber,
                meter.Unit?.ToString() ?? "Unknown",
                meter.Quantity?.ToString() ?? "Unknown",
                meter.Readings.ReadingType?.ToString() ?? "Unknown",
                request.Aggregation.ToString(),
                meter.Readings.PeriodStart,
                meter.Readings.PeriodEnd,
                from,
                to,
                new(values, page, size, total),
                null);
        }
        var aggregated = await AggregateAsync(
            scoped,
            request.Aggregation,
            Desc(request.SortDirection),
            ct);
        return new(
            meter.MeterId,
            meter.MeterNumber,
            meter.Unit?.ToString() ?? "Unknown",
            meter.Quantity?.ToString() ?? "Unknown",
            meter.Readings.ReadingType?.ToString() ?? "Unknown",
            request.Aggregation.ToString(),
            meter.Readings.PeriodStart,
            meter.Readings.PeriodEnd,
            from,
            to,
            null,
            aggregated);
    }

    private static MeterSummaryDto ToMeterSummary(
        MeterCanonicalSnapshot meter)
    {
        var readings = meter.Readings;
        return new MeterSummaryDto(
            meter.MeterId,
            meter.MeterNumber,
            meter.Name,
            meter.Medium?.ToString() ?? "Unknown",
            meter.Unit?.ToString() ?? "Unknown",
            meter.Direction?.ToString() ?? "Unknown",
            meter.BuildingId,
            meter.BuildingNumber,
            meter.BuildingName,
            meter.CustomerNumber,
            meter.CustomerName,
            readings.AnnualValue,
            readings.AnnualValueStatus.ToString(),
            readings.MeasurementCount,
            readings.PeriodStart,
            readings.PeriodEnd,
            meter.Quality.Level.ToString(),
            meter.Quality.CompletenessPercentage,
            false)
        {
            QualityLevel = meter.Quality.Level.ToString(),
            Quantity = meter.Quantity?.ToString(),
            ReadingType = readings.ReadingType?.ToString(),
            IntervalSeconds = readings.IntervalSeconds,
            AnnualValueStatus = readings.AnnualValueStatus.ToString(),
            AnnualValueUnit = readings.Unit?.ToString(),
            AnnualValueReferenceYear =
                readings.AnnualValueReferenceYear
        };
    }

    private static PagedResult<T> Page<T>(
        IEnumerable<T> source,
        int requestedPage,
        int requestedSize)
    {
        var (page, size) = NormalizePage(
            requestedPage,
            requestedSize);
        var values = source.ToArray();
        return new(
            values
                .Skip((page - 1) * size)
                .Take(size)
                .ToArray(),
            page,
            size,
            values.Length);
    }

    private static IOrderedEnumerable<T> Sort<T>(
        IEnumerable<T> source,
        string direction,
        Func<T, string> key) =>
        Desc(direction)
            ? source.OrderByDescending(
                key,
                StringComparer.OrdinalIgnoreCase)
            : source.OrderBy(
                key,
                StringComparer.OrdinalIgnoreCase);

    private static bool Contains(string? value, string search) =>
        value?.Contains(
            search,
            StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<IReadOnlyList<AggregatedMeterReadingDto>>
        AggregateAsync(
            IQueryable<MeterReading> source,
            MeterReadingAggregation aggregation,
            bool descending,
            CancellationToken ct)
    {
        var grouped = aggregation switch
        {
            MeterReadingAggregation.FifteenMinutes => source.GroupBy(x =>
                new
                {
                    x.Timestamp.Year,
                    x.Timestamp.Month,
                    x.Timestamp.Day,
                    x.Timestamp.Hour,
                    Minute = x.Timestamp.Minute / 15,
                    x.ReadingType
                }),
            MeterReadingAggregation.Hour => source.GroupBy(x =>
                new
                {
                    x.Timestamp.Year,
                    x.Timestamp.Month,
                    x.Timestamp.Day,
                    x.Timestamp.Hour,
                    Minute = 0,
                    x.ReadingType
                }),
            MeterReadingAggregation.Day => source.GroupBy(x =>
                new
                {
                    x.Timestamp.Year,
                    x.Timestamp.Month,
                    x.Timestamp.Day,
                    Hour = 0,
                    Minute = 0,
                    x.ReadingType
                }),
            MeterReadingAggregation.Month => source.GroupBy(x =>
                new
                {
                    x.Timestamp.Year,
                    x.Timestamp.Month,
                    Day = 1,
                    Hour = 0,
                    Minute = 0,
                    x.ReadingType
                }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(aggregation))
        };
        var rows = await grouped
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                group.Key.Hour,
                group.Key.Minute,
                group.Key.ReadingType,
                Minimum = group.Min(x => x.Value),
                Maximum = group.Max(x => x.Value),
                Average = group.Average(x => x.Value),
                Sum = group.Key.ReadingType ==
                      MeterReadingType.IntervalValue
                    ? (decimal?)group.Sum(x => x.Value)
                    : null,
                First = group.OrderBy(x => x.Timestamp)
                    .Select(x => x.Value)
                    .First(),
                Last = group.OrderByDescending(x => x.Timestamp)
                    .Select(x => x.Value)
                    .First(),
                Count = group.Count()
            })
            .ToListAsync(ct);
        var result = rows.Select(x =>
            new AggregatedMeterReadingDto(
                new DateTime(
                    x.Year,
                    x.Month,
                    x.Day,
                    x.Hour,
                    x.Minute *
                    (aggregation ==
                     MeterReadingAggregation.FifteenMinutes
                        ? 15
                        : 1),
                    0,
                    DateTimeKind.Utc),
                x.ReadingType.ToString(),
                x.Minimum,
                x.Maximum,
                x.Average,
                x.Sum,
                x.First,
                x.Last,
                x.Last - x.First,
                x.Count));
        return (descending
                ? result.OrderByDescending(x => x.BucketStart)
                : result.OrderBy(x => x.BucketStart))
            .ToArray();
    }

    private static (int Page, int Size) NormalizePage(
        int page,
        int size) =>
        (Math.Max(1, page), Math.Clamp(size, 1, MaximumPageSize));

    private static bool Desc(string value) =>
        value.Equals("desc", StringComparison.OrdinalIgnoreCase);

    private static DateTime? Utc(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } utc => utc,
            { Kind: DateTimeKind.Local } local =>
                local.ToUniversalTime(),
            var unspecified => DateTime.SpecifyKind(
                unspecified.Value,
                DateTimeKind.Utc)
        };
}
