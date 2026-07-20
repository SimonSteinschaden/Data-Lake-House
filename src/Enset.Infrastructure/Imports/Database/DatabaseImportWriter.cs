using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;
using Enset.Domain.Customers;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.Buildings;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Database;

public sealed class DatabaseImportWriter : IImportWriter
{
    private readonly EnsetDbContext _dbContext;

    public DatabaseImportWriter(EnsetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ImportWriterType WriterType => ImportWriterType.Database;

    public async Task WriteAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidateContext(context);

        if (context.TargetMode == ImportTargetMode.Replace)
        {
            throw new NotSupportedException(
                "Replace imports are not supported by the database writer.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var customers = await UpsertCustomersAsync(
                context.Customers,
                cancellationToken);

            var buildings = await UpsertBuildingsAsync(
                context.Buildings,
                cancellationToken);

            await EnsureCustomerBuildingAssignmentsAsync(
                context,
                customers,
                buildings,
                cancellationToken);

            var meters = await UpsertMetersAsync(
                context.Meters,
                buildings,
                cancellationToken);

            await InsertMeterReadingsAsync(
                context,
                meters,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateContext(ImportWriteContext context)
    {
        if (context.Report is null)
        {
            throw new InvalidOperationException(
                "Database import requires an ImportReport.");
        }

        if (context.ImportId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Database import requires a valid ImportId.");
        }

        if (string.IsNullOrWhiteSpace(context.UserId))
        {
            throw new InvalidOperationException(
                "Database import requires a UserId.");
        }
    }

    private async Task<Dictionary<string, Customer>> UpsertCustomersAsync(
        IReadOnlyCollection<CustomerImportDto> source,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Customer>(
            StringComparer.OrdinalIgnoreCase);

        var customerNumbers = source
            .Select(x => NormalizeRequired(
                x.ExternalCustomerId,
                "ExternalCustomerId"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingCustomers = await _dbContext.Customers
            .Where(x => customerNumbers.Contains(x.CustomerNumber))
            .ToDictionaryAsync(
                x => x.CustomerNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        foreach (var dto in source)
        {
            var customerNumber = NormalizeRequired(
                dto.ExternalCustomerId,
                "ExternalCustomerId");

            if (!existingCustomers.TryGetValue(
                    customerNumber,
                    out var customer))
            {
                customer = new Customer
                {
                    CustomerNumber = customerNumber,
                    Type = CustomerType.Unknown
                };

                _dbContext.Customers.Add(customer);
                existingCustomers[customerNumber] = customer;
            }

            customer.Name = FirstNonEmpty(
                dto.CompanyName,
                dto.ContactPerson,
                customerNumber);

            customer.LegalName = NormalizeOptional(dto.CompanyName);
            customer.CompanyRegistrationNumber =
                NormalizeOptional(dto.CompanyRegistrationNumber);
            customer.VatIdentificationNumber =
                NormalizeOptional(dto.VatNumber);
            customer.Email = NormalizeOptional(dto.Email);
            customer.Phone = NormalizeOptional(dto.Phone);
            customer.Street = NormalizeOptional(dto.Street);
            customer.HouseNumber = NormalizeOptional(dto.HouseNumber);
            customer.PostalCode = NormalizeOptional(dto.PostalCode);
            customer.City = NormalizeOptional(dto.City);
            customer.CountryCode = NormalizeCountryCode(dto.Country);
            customer.IsActive = true;

            result[customerNumber] = customer;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<Dictionary<string, Building>> UpsertBuildingsAsync(
        IReadOnlyCollection<BuildingImportDto> source,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Building>(
            StringComparer.OrdinalIgnoreCase);

        var buildingNumbers = source
            .Select(x => NormalizeRequired(
                x.ExternalBuildingId,
                "ExternalBuildingId"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingBuildings = await _dbContext.Buildings
            .Where(x => buildingNumbers.Contains(x.BuildingNumber))
            .ToDictionaryAsync(
                x => x.BuildingNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        foreach (var dto in source)
        {
            var buildingNumber = NormalizeRequired(
                dto.ExternalBuildingId,
                "ExternalBuildingId");

            if (!existingBuildings.TryGetValue(
                    buildingNumber,
                    out var building))
            {
                building = new Building
                {
                    BuildingNumber = buildingNumber
                };

                _dbContext.Buildings.Add(building);
                existingBuildings[buildingNumber] = building;
            }

            building.Name = FirstNonEmpty(
                dto.BuildingName,
                buildingNumber);

            building.ExternalIdentifier = buildingNumber;
            building.IsActive = true;

            result[buildingNumber] = building;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task EnsureCustomerBuildingAssignmentsAsync(
        ImportWriteContext context,
        IReadOnlyDictionary<string, Customer> customers,
        IReadOnlyDictionary<string, Building> buildings,
        CancellationToken cancellationToken)
    {
        foreach (var dto in context.Buildings)
        {
            var buildingNumber = NormalizeRequired(
                dto.ExternalBuildingId,
                "ExternalBuildingId");

            var customerNumber = ResolveCustomerNumber(
                dto.ExternalCustomerId,
                context.Customers);

            if (!customers.TryGetValue(customerNumber, out var customer))
            {
                throw new InvalidOperationException(
                    $"Customer '{customerNumber}' was not found.");
            }

            if (!buildings.TryGetValue(buildingNumber, out var building))
            {
                throw new InvalidOperationException(
                    $"Building '{buildingNumber}' was not found.");
            }

            var assignmentExists =
                await _dbContext.CustomerBuildingAssignments.AnyAsync(
                    x =>
                        x.CustomerId == customer.Id &&
                        x.BuildingId == building.Id &&
                        x.ValidTo == null,
                    cancellationToken);

            if (assignmentExists)
            {
                continue;
            }

            _dbContext.CustomerBuildingAssignments.Add(
                new CustomerBuildingAssignment
                {
                    CustomerId = customer.Id,
                    BuildingId = building.Id,
                    Role = CustomerBuildingRole.Unknown,
                    ValidFrom = context.Timestamp.ToUniversalTime(),
                    ValidTo = null,
                    IsPrimary = false
                });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Meter>> UpsertMetersAsync(
        IReadOnlyCollection<MeterImportDto> source,
        IReadOnlyDictionary<string, Building> importedBuildings,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Meter>(
            StringComparer.OrdinalIgnoreCase);

        var meterNumbers = source
            .Select(x => NormalizeRequired(
                x.MeterNumber,
                "MeterNumber"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingMeters = await _dbContext.Meters
            .Where(x => meterNumbers.Contains(x.MeterNumber))
            .ToDictionaryAsync(
                x => x.MeterNumber,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        foreach (var dto in source)
        {
            var meterNumber = NormalizeRequired(
                dto.MeterNumber,
                "MeterNumber");

            if (!existingMeters.TryGetValue(meterNumber, out var meter))
            {
                meter = new Meter
                {
                    MeterNumber = meterNumber,
                    Medium = MeterMedium.Unknown,
                    Quantity = MeterQuantity.Unknown,
                    Direction = MeterDirection.Unknown,
                    Type = MeterType.Unknown
                };

                _dbContext.Meters.Add(meter);
                existingMeters[meterNumber] = meter;
            }

            meter.Name = FirstNonEmpty(
                dto.ProfileName,
                meterNumber);

            meter.Unit = ParseMeterUnit(dto.Unit);
            meter.BuildingId = await ResolveBuildingIdAsync(
                dto,
                importedBuildings,
                cancellationToken);

            meter.IsActive = true;

            result[meterNumber] = meter;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<Guid> ResolveBuildingIdAsync(
        MeterImportDto dto,
        IReadOnlyDictionary<string, Building> importedBuildings,
        CancellationToken cancellationToken)
    {
        if (dto.BuildingId.HasValue)
        {
            var exists = await _dbContext.Buildings.AnyAsync(
                x => x.Id == dto.BuildingId.Value,
                cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Building '{dto.BuildingId}' was not found.");
            }

            return dto.BuildingId.Value;
        }

        var buildingNumber = NormalizeRequired(
            dto.ExternalBuildingId,
            $"ExternalBuildingId for meter '{dto.MeterNumber}'");

        if (importedBuildings.TryGetValue(
                buildingNumber,
                out var importedBuilding))
        {
            return importedBuilding.Id;
        }

        var existingBuilding = await _dbContext.Buildings
            .SingleOrDefaultAsync(
                x => x.BuildingNumber == buildingNumber,
                cancellationToken);

        return existingBuilding?.Id
            ?? throw new InvalidOperationException(
                $"Building '{buildingNumber}' for meter " +
                $"'{dto.MeterNumber}' was not found.");
    }

    private async Task InsertMeterReadingsAsync(
        ImportWriteContext context,
        IReadOnlyDictionary<string, Meter> meters,
        CancellationToken cancellationToken)
    {
        foreach (var dto in context.MeterReadings)
        {
            if (dto.HasError || dto.Value is null)
            {
                continue;
            }

            var meterNumber = NormalizeRequired(
                dto.MeterNumber,
                "MeterNumber");

            if (!meters.TryGetValue(meterNumber, out var meter))
            {
                meter = await _dbContext.Meters.SingleOrDefaultAsync(
                    x => x.MeterNumber == meterNumber,
                    cancellationToken);

                if (meter is null)
                {
                    throw new InvalidOperationException(
                        $"Meter '{meterNumber}' was not found.");
                }
            }

            var timestamp = NormalizeUtc(dto.Timestamp);

            var readingExists = await _dbContext.MeterReadings.AnyAsync(
                x =>
                    x.MeterId == meter.Id &&
                    x.Timestamp == timestamp,
                cancellationToken);

            if (readingExists)
            {
                continue;
            }

            _dbContext.MeterReadings.Add(
                new MeterReading
                {
                    MeterId = meter.Id,
                    Timestamp = timestamp,
                    Value = dto.Value.Value,
                    ReadingType = MeterReadingType.Unknown,
                    QualityFlag = ParseDataQuality(dto.QualityFlag),
                    SourceImportJobId = context.ImportId
                });
        }
    }

    private static string ResolveCustomerNumber(
        string? externalCustomerId,
        IReadOnlyCollection<CustomerImportDto> customers)
    {
        if (!string.IsNullOrWhiteSpace(externalCustomerId))
        {
            return externalCustomerId.Trim();
        }

        if (customers.Count == 1)
        {
            return NormalizeRequired(
                customers.Single().ExternalCustomerId,
                "ExternalCustomerId");
        }

        throw new InvalidOperationException(
            "A building requires ExternalCustomerId when " +
            "multiple customers are contained in the import.");
    }

    private static MeterUnit ParseMeterUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MeterUnit.Unknown;
        }

        var normalized = value
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("³", "3")
            .ToLowerInvariant();

        return normalized switch
        {
            "wh" => MeterUnit.Wh,
            "kwh" => MeterUnit.KWh,
            "mwh" => MeterUnit.MWh,
            "w" => MeterUnit.W,
            "kw" => MeterUnit.KW,
            "mw" => MeterUnit.MW,
            "m3" => MeterUnit.CubicMeter,
            "m3/h" => MeterUnit.CubicMeterPerHour,
            "l" => MeterUnit.Liter,
            "l/s" => MeterUnit.LiterPerSecond,
            "°c" or "c" => MeterUnit.Celsius,
            "k" => MeterUnit.Kelvin,
            "pa" => MeterUnit.Pascal,
            "bar" => MeterUnit.Bar,
            "v" => MeterUnit.Volt,
            "a" => MeterUnit.Ampere,
            "hz" => MeterUnit.Hertz,
            "w/m2" => MeterUnit.WattPerSquareMeter,
            "m/s" => MeterUnit.MeterPerSecond,
            "%" => MeterUnit.Percent,
            _ => MeterUnit.Unknown
        };
    }

    private static DataQuality ParseDataQuality(int? qualityFlag)
    {
        if (!qualityFlag.HasValue ||
            !Enum.IsDefined(typeof(DataQuality), qualityFlag.Value))
        {
            return DataQuality.Unknown;
        }

        return (DataQuality)qualityFlag.Value;
    }

    private static DateTime NormalizeUtc(DateTime timestamp)
    {
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
            _ => timestamp
        };
    }

    private static string NormalizeRequired(
        string? value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Import field '{fieldName}' is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "AT";
        }

        var normalized = value.Trim().ToUpperInvariant();

        return normalized switch
        {
            "AUSTRIA" or "ÖSTERREICH" => "AT",
            "GERMANY" or "DEUTSCHLAND" => "DE",
            "SWITZERLAND" or "SCHWEIZ" => "CH",
            _ when normalized.Length == 2 => normalized,
            _ => "AT"
        };
    }

    private static string FirstNonEmpty(
        params string?[] values)
    {
        return values
            .First(x => !string.IsNullOrWhiteSpace(x))!
            .Trim();
    }
}
/*TODO:
Replace
EnergySystem-Import
EnergyCommunity-Import
Mobilitätsimport
fachliche Ableitung von MeterMedium, MeterQuantity, MeterType
Aktualisierung eines bereits vorhandenen Messwerts
Historisierung geänderter Gebäudeattribute
*/