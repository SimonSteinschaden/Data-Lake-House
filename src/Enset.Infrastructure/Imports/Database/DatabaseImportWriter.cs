using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;
using Enset.Domain.Customers;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.Buildings;
using Enset.Domain.Common;
using Enset.Domain.Geography;
using Enset.Infrastructure.Persistence;
using Enset.Application.Crud;
using Enset.Infrastructure.Crud;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Database;

public sealed class DatabaseImportWriter : IImportWriter
{
    private readonly EnsetDbContext _dbContext;
    private readonly IBuildingNumberGenerator _buildingNumbers;

    public DatabaseImportWriter(
        EnsetDbContext dbContext,
        IBuildingNumberGenerator? buildingNumbers = null)
    {
        _dbContext = dbContext;
        _buildingNumbers =
            buildingNumbers ?? new EfBuildingNumberGenerator(dbContext);
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
                context,
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

            var rawReadings = await InsertRawMeterReadingsAsync(
                context,
                meters,
                cancellationToken);

            await InsertCuratedMeterReadingsAsync(
                context,
                rawReadings,
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
        ImportWriteContext context,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Building>(
            StringComparer.OrdinalIgnoreCase);

        var externalIdentifiers = source
            .Select(x => NormalizeRequired(
                x.ExternalBuildingId,
                "ExternalBuildingId"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingBuildings = await _dbContext.Buildings
            .Include(x => x.Versions)
                .ThenInclude(x => x.Address)
                    .ThenInclude(x => x!.PostalCodeArea)
            .Include(x => x.Versions)
                .ThenInclude(x => x.Address)
                    .ThenInclude(x => x!.Country)
            .Where(x => x.ExternalIdentifier != null &&
                externalIdentifiers.Contains(x.ExternalIdentifier))
            .ToDictionaryAsync(
                x => x.ExternalIdentifier!,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        foreach (var dto in source)
        {
            var externalIdentifier = NormalizeRequired(
                dto.ExternalBuildingId,
                "ExternalBuildingId");

            if (!existingBuildings.TryGetValue(
                    externalIdentifier,
                    out var building))
            {
                building = new Building
                {
                    BuildingNumber =
                        await _buildingNumbers.NextAsync(cancellationToken),
                    ExternalIdentifier = externalIdentifier
                };

                _dbContext.Buildings.Add(building);
                existingBuildings[externalIdentifier] = building;
            }

            building.Name = FirstNonEmpty(
                dto.BuildingName,
                building.Name,
                externalIdentifier);

            building.ExternalIdentifier = externalIdentifier;
            building.IsActive = true;

            if (HasBuildingVersionData(dto))
            {
                await AddBuildingVersionAsync(
                    building,
                    dto,
                    context,
                    cancellationToken);
            }

            result[externalIdentifier] = building;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task AddBuildingVersionAsync(
        Building building,
        BuildingImportDto dto,
        ImportWriteContext context,
        CancellationToken cancellationToken)
    {
        var previous = building.Versions
            .Where(x => x.ValidTo == null)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault();
        var address = await BuildImportedAddressAsync(
            dto,
            previous?.Address,
            cancellationToken);
        var validFrom = context.Timestamp.ToUniversalTime();

        if (previous is not null)
            previous.ValidTo = validFrom;

        var version = new BuildingVersion
        {
            BuildingId = building.Id,
            VersionNumber = (previous?.VersionNumber ?? 0) + 1,
            ValidFrom = validFrom,
            RecordedAt = validFrom,
            ChangeReason = "Import",
            Address = address,
            AddressId = address?.Id,
            CadastralMunicipality = previous?.CadastralMunicipality,
            PropertyNumber = previous?.PropertyNumber,
            BuildingRegistryIdentifier =
                previous?.BuildingRegistryIdentifier,
            PrimaryUseType = ParseEnum<PrimaryUseType>(dto.UsageType)
                ?? previous?.PrimaryUseType,
            BuildingCategory =
                ParseEnum<BuildingCategory>(dto.BuildingType)
                ?? previous?.BuildingCategory,
            OwnershipType = previous?.OwnershipType,
            IsResidential = previous?.IsResidential ?? false,
            IsCommercial = previous?.IsCommercial ?? false,
            IsPublic = previous?.IsPublic ?? false,
            HasMixedUse = previous?.HasMixedUse ?? false,
            YearOfConstruction =
                dto.ConstructionYear ?? previous?.YearOfConstruction,
            YearOfLastMajorRenovation =
                dto.RenovationYear ??
                previous?.YearOfLastMajorRenovation,
            GrossFloorAreaM2 =
                dto.GrossFloorAreaM2 ?? previous?.GrossFloorAreaM2,
            NetFloorAreaM2 =
                dto.NetFloorAreaM2 ?? previous?.NetFloorAreaM2,
            ConditionedFloorAreaM2 =
                dto.ConditionedFloorAreaM2 ??
                previous?.ConditionedFloorAreaM2,
            HeatedFloorAreaM2 =
                dto.HeatedFloorAreaM2 ?? previous?.HeatedFloorAreaM2,
            CooledFloorAreaM2 =
                dto.CooledFloorAreaM2 ?? previous?.CooledFloorAreaM2,
            BuildingVolumeM3 =
                dto.BuildingVolumeM3 ?? previous?.BuildingVolumeM3,
            NumberOfFloors =
                dto.NumberOfFloors ?? previous?.NumberOfFloors,
            NumberOfUsageUnits = previous?.NumberOfUsageUnits,
            IsProtectedBuilding =
                previous?.IsProtectedBuilding ?? false,
            IsTemporaryBuilding =
                previous?.IsTemporaryBuilding ?? false,
            DataOrigin = DataOrigin.Imported,
            LastImportId = context.ImportId,
            LastModifiedSource = LastModifiedSource.Import
        };

        _dbContext.BuildingVersions.Add(version);
        building.Versions.Add(version);
    }

    private async Task<Address?> BuildImportedAddressAsync(
        BuildingImportDto dto,
        Address? previous,
        CancellationToken cancellationToken)
    {
        var countryCode = NormalizeOptional(dto.Country);
        var country = countryCode is null
            ? previous?.Country
            : await _dbContext.Countries.SingleOrDefaultAsync(
                x => x.IsoCode2 == NormalizeCountryCode(countryCode),
                cancellationToken);
        if (country is null)
            return previous;

        PostalCodeArea? postalCodeArea = previous?.PostalCodeArea;
        var postalCode = NormalizeOptional(dto.PostalCode);
        if (postalCode is not null)
        {
            postalCodeArea = await _dbContext.PostalCodeAreas
                .SingleOrDefaultAsync(
                    x => x.CountryId == country.Id &&
                         x.Code == postalCode,
                    cancellationToken);
            if (postalCodeArea is null)
            {
                postalCodeArea = new PostalCodeArea
                {
                    CountryId = country.Id,
                    Code = postalCode,
                    Name = NormalizeOptional(dto.City),
                    DataOrigin = DataOrigin.Imported,
                    LastModifiedSource = LastModifiedSource.Import
                };
                _dbContext.PostalCodeAreas.Add(postalCodeArea);
            }
        }

        return new Address
        {
            CountryId = country.Id,
            Country = country,
            PostalCodeAreaId = postalCodeArea?.Id,
            PostalCodeArea = postalCodeArea,
            Street = NormalizeOptional(dto.Street) ?? previous?.Street,
            HouseNumber =
                NormalizeOptional(dto.HouseNumber) ??
                previous?.HouseNumber,
            AddressAddition =
                NormalizeOptional(dto.AddressAddition) ??
                previous?.AddressAddition,
            City = NormalizeOptional(dto.City) ?? previous?.City,
            DataOrigin = DataOrigin.Imported,
            LastModifiedSource = LastModifiedSource.Import
        };
    }

    private static bool HasBuildingVersionData(BuildingImportDto dto) =>
        new[]
        {
            dto.Street, dto.HouseNumber, dto.AddressAddition,
            dto.PostalCode, dto.City, dto.Country, dto.BuildingType,
            dto.UsageType, dto.BuildingState
        }.Any(value => !string.IsNullOrWhiteSpace(value)) ||
        dto.ConstructionYear.HasValue ||
        dto.RenovationYear.HasValue ||
        dto.GrossFloorAreaM2.HasValue ||
        dto.NetFloorAreaM2.HasValue ||
        dto.ConditionedFloorAreaM2.HasValue ||
        dto.HeatedFloorAreaM2.HasValue ||
        dto.CooledFloorAreaM2.HasValue ||
        dto.BuildingVolumeM3.HasValue ||
        dto.NumberOfFloors.HasValue;

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(NormalizeOptional(value), true, out var parsed)
            ? parsed
            : null;

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
                dto.Name,
                dto.ProfileName,
                meter.Name,
                meterNumber);

            var importedUnit = ParseMeterUnit(dto.Unit);
            if (importedUnit != MeterUnit.Unknown ||
                meter.Unit == MeterUnit.Unknown)
            {
                meter.Unit = importedUnit;
            }
            var importedQuantity = DeriveMeterQuantity(importedUnit);
            if (importedQuantity != MeterQuantity.Unknown ||
                meter.Quantity == MeterQuantity.Unknown)
            {
                meter.Quantity = importedQuantity;
            }
            if (dto.AnnualValue.HasValue)
            {
                meter.AnnualValue = dto.AnnualValue;
                meter.AnnualValueOrigin = "ImportedAnnualTotal";
                meter.AnnualValueReferenceYear =
                    dto.AnnualValueReferenceYear;
            }
            meter.Medium = dto.ProfileName switch
            {
                nameof(ImportMedium.Electricity) => MeterMedium.Electricity,
                nameof(ImportMedium.Heat) => MeterMedium.Heat,
                _ => meter.Medium
            };
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

    private async Task<Guid?> ResolveBuildingIdAsync(
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

        if (dto.AllowUnassignedBuilding &&
            string.IsNullOrWhiteSpace(dto.ExternalBuildingId))
        {
            return null;
        }

        var externalBuildingId = NormalizeRequired(
            dto.ExternalBuildingId,
            $"ExternalBuildingId for meter '{dto.MeterNumber}'");

        if (importedBuildings.TryGetValue(
                externalBuildingId,
                out var importedBuilding))
        {
            return importedBuilding.Id;
        }

        var existingBuilding = await _dbContext.Buildings
            .SingleOrDefaultAsync(
                x => x.ExternalIdentifier == externalBuildingId,
                cancellationToken);

        return existingBuilding?.Id
            ?? throw new InvalidOperationException(
                $"Building '{externalBuildingId}' for meter " +
                $"'{dto.MeterNumber}' was not found.");
    }

    private async Task<IReadOnlyList<RawReadingCandidate>>
        InsertRawMeterReadingsAsync(
        ImportWriteContext context,
        IReadOnlyDictionary<string, Meter> meters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(meters);

        var resolvedMeters = new Dictionary<string, Meter>(
            meters,
            StringComparer.OrdinalIgnoreCase);
        var resolvedMetersById = meters.Values
            .ToDictionary(meter => meter.Id);
        var sourceMeterIds = context.MeterReadings
            .Where(reading => reading.MeterId.HasValue)
            .Select(reading => reading.MeterId!.Value)
            .Distinct()
            .Where(id => !resolvedMetersById.ContainsKey(id))
            .ToList();
        if (sourceMeterIds.Count > 0)
        {
            var existingMetersById = await _dbContext.Meters
                .Where(meter => sourceMeterIds.Contains(meter.Id))
                .ToListAsync(cancellationToken);
            foreach (var meter in existingMetersById)
            {
                resolvedMetersById[meter.Id] = meter;
                resolvedMeters[meter.MeterNumber] = meter;
            }
        }
        var sourceMeterNumbers = context.MeterReadings
            .Select(reading => NormalizeOptional(
                reading.MeterNumberRaw ?? reading.MeterNumber))
            .Where(number => number is not null)
            .Select(number => number!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(number => !resolvedMeters.ContainsKey(number))
            .ToList();

        if (sourceMeterNumbers.Count > 0)
        {
            var existingMeters = await _dbContext.Meters
                .Where(meter => sourceMeterNumbers.Contains(meter.MeterNumber))
                .ToListAsync(cancellationToken);
            foreach (var meter in existingMeters)
                resolvedMeters[meter.MeterNumber] = meter;
        }

        var result = new List<RawReadingCandidate>(
            context.MeterReadings.Count);

        foreach (var dto in context.MeterReadings)
        {
            Meter? meter = null;
            if (dto.MeterId.HasValue)
                resolvedMetersById.TryGetValue(dto.MeterId.Value, out meter);
            if (dto.MeterId.HasValue && meter is null)
                throw new InvalidOperationException(
                    $"Meter '{dto.MeterId}' was not found.");
            var effectiveMeterNumber =
                NormalizeOptional(dto.MeterNumber) ??
                meter?.MeterNumber;
            var normalizedMeterNumber =
                NormalizeOptional(effectiveMeterNumber);
            if (meter is null)
                resolvedMeters.TryGetValue(
                    normalizedMeterNumber ?? string.Empty,
                    out meter);

            var raw = new ImportedMeterReading
            {
                ImportId = context.ImportId,
                MeterId = meter?.Id,
                MeterNumberRaw = dto.MeterNumberRaw,
                TimestampRaw = dto.TimestampRaw,
                ValueRaw = dto.ValueRaw,
                QualityRaw = dto.QualityRaw,
                Timestamp = dto.Timestamp.HasValue
                    ? NormalizeUtc(dto.Timestamp.Value)
                    : null,
                Value = dto.Value,
                Quality = dto.QualityFlag,
                RowNumber = dto.RowNumber,
                SourceName = context.Report?.SourceFile?.FileName,
                ParsingError = dto.ErrorMessage ?? dto.ParsingError
            };
            _dbContext.ImportedMeterReadings.Add(raw);
            result.Add(new RawReadingCandidate(dto, raw, meter));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task InsertCuratedMeterReadingsAsync(
        ImportWriteContext context,
        IReadOnlyList<RawReadingCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var eligible = candidates
            .Where(candidate =>
                !candidate.Dto.HasError &&
                candidate.Raw.MeterId.HasValue &&
                candidate.Raw.Timestamp.HasValue &&
                candidate.Raw.Value.HasValue)
            .ToList();
        if (eligible.Count == 0)
            return;

        var inferredIntervals = eligible
            .GroupBy(candidate => candidate.Raw.MeterId!.Value)
            .ToDictionary(
                group => group.Key,
                group => InferFixedIntervalSeconds(
                    group.Select(candidate =>
                        candidate.Raw.Timestamp!.Value)));

        var meterIds = eligible
            .Select(candidate => candidate.Raw.MeterId!.Value)
            .Distinct()
            .ToList();
        var timestamps = eligible
            .Select(candidate => candidate.Raw.Timestamp!.Value)
            .Distinct()
            .ToList();
        var knownKeys = (await _dbContext.MeterReadings
                .AsNoTracking()
                .Where(reading =>
                    meterIds.Contains(reading.MeterId) &&
                    timestamps.Contains(reading.Timestamp))
                .Select(reading => new
                {
                    reading.MeterId,
                    reading.Timestamp
                })
                .ToListAsync(cancellationToken))
            .Select(reading => (reading.MeterId, reading.Timestamp))
            .ToHashSet();

        foreach (var candidate in eligible)
        {
            var meterId = candidate.Raw.MeterId!.Value;
            var timestamp = candidate.Raw.Timestamp!.Value;
            if (!knownKeys.Add((meterId, timestamp)))
                continue;

            _dbContext.MeterReadings.Add(new MeterReading
            {
                MeterId = meterId,
                Timestamp = timestamp,
                Value = candidate.Raw.Value!.Value,
                ReadingType = candidate.Dto.ReadingType,
                IntervalSeconds = candidate.Dto.IntervalSeconds ??
                    inferredIntervals[meterId],
                QualityFlag = ParseDataQuality(candidate.Raw.Quality),
                SourceRawReadingId = candidate.Raw.Id,
                SourceImportJobId = context.ImportId
            });

            var importedUnit = ParseMeterUnit(candidate.Dto.Unit);
            var importedQuantity = DeriveMeterQuantity(importedUnit);
            if (candidate.Meter is not null)
            {
                if (candidate.Meter.Unit == MeterUnit.Unknown &&
                    importedUnit != MeterUnit.Unknown)
                {
                    candidate.Meter.Unit = importedUnit;
                }
                if (candidate.Meter.Quantity == MeterQuantity.Unknown &&
                    importedQuantity != MeterQuantity.Unknown)
                {
                    candidate.Meter.Quantity = importedQuantity;
                }
            }
        }
    }

    private sealed record RawReadingCandidate(
        MeterReadingImportDto Dto,
        ImportedMeterReading Raw,
        Meter? Meter);

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

    private static MeterQuantity DeriveMeterQuantity(MeterUnit unit) =>
        unit switch
        {
            MeterUnit.Wh or MeterUnit.KWh or MeterUnit.MWh =>
                MeterQuantity.Energy,
            MeterUnit.W or MeterUnit.KW or MeterUnit.MW =>
                MeterQuantity.Power,
            MeterUnit.CubicMeter or MeterUnit.Liter =>
                MeterQuantity.Volume,
            MeterUnit.CubicMeterPerHour or
                MeterUnit.LiterPerSecond => MeterQuantity.Flow,
            MeterUnit.Celsius or MeterUnit.Kelvin =>
                MeterQuantity.Temperature,
            MeterUnit.Pascal or MeterUnit.Bar =>
                MeterQuantity.Pressure,
            MeterUnit.Volt => MeterQuantity.Voltage,
            MeterUnit.Ampere => MeterQuantity.Current,
            MeterUnit.Hertz => MeterQuantity.Frequency,
            MeterUnit.WattPerSquareMeter =>
                MeterQuantity.Irradiance,
            MeterUnit.MeterPerSecond => MeterQuantity.WindSpeed,
            _ => MeterQuantity.Unknown
        };

    private static int? InferFixedIntervalSeconds(
        IEnumerable<DateTime> timestamps)
    {
        var ordered = timestamps.Distinct().OrderBy(x => x).ToList();
        if (ordered.Count < 2)
            return null;

        var gaps = ordered
            .Zip(ordered.Skip(1), (left, right) =>
                (int)(right - left).TotalSeconds)
            .Where(seconds => seconds > 0)
            .Distinct()
            .ToList();
        return gaps.Count == 1 ? gaps[0] : null;
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
