using Enset.Application.Crud;
using Enset.Application.Authorization;
using Enset.Domain.Common;
using Enset.Domain.Customers;
using Enset.Domain.Buildings;
using Enset.Domain.Energy;
using Enset.Domain.Data;
using Enset.Domain.Curation;
using Enset.Domain.Geography;
using Enset.Infrastructure.Persistence;
using Enset.Application.Quality;
using Enset.Infrastructure.Quality;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Crud;

public sealed class EfEntityCrudService(
    EnsetDbContext db,
    IDataAccessScope? scope = null,
    IBuildingNumberGenerator? buildingNumbers = null,
    IQualityInvalidationService? qualityInvalidation = null,
    ICurrentUserContext? currentUser = null) : IEntityCrudService
{
    private readonly IBuildingNumberGenerator _buildingNumbers =
        buildingNumbers ?? new EfBuildingNumberGenerator(db);
    private readonly IQualityInvalidationService _qualityInvalidation =
        qualityInvalidation ?? new EfQualityInvalidationService(db, currentUser ?? new CurrentUserContext());
    public async Task<EntityMutationResult> CreateCustomerAsync(CustomerWriteModel m, CancellationToken ct)
    {
        Required((nameof(m.CustomerNumber), m.CustomerNumber), (nameof(m.Name), m.Name));
        if (await db.Customers.AnyAsync(x => x.CustomerNumber == m.CustomerNumber.Trim(), ct))
            throw new CrudConflictException("Ein Kunde mit dieser Kundennummer existiert bereits.");
        var entity = new Customer { CustomerNumber = m.CustomerNumber.Trim(), Name = m.Name.Trim(),
            LegalName = Trim(m.LegalName), Email = Trim(m.Email), Phone = Trim(m.Phone),
            ContactPerson = Trim(m.ContactPerson), Street = Trim(m.Street),
            HouseNumber = Trim(m.HouseNumber), PostalCode = Trim(m.PostalCode), City = Trim(m.City),
            CountryCode = string.IsNullOrWhiteSpace(m.CountryCode) ? "AT" : m.CountryCode.Trim().ToUpperInvariant(),
            Type = Parse<CustomerType>(m.Type, nameof(m.Type)) };
        db.Customers.Add(entity);
        await Save(ct);
        return Result(entity);
    }

    public async Task<EntityMutationResult> UpdateCustomerAsync(Guid id, CustomerWriteModel m, CancellationToken ct)
    {
        Required((nameof(m.CustomerNumber), m.CustomerNumber), (nameof(m.Name), m.Name));
        var e = await Customers().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Kunde");
        Concurrency(e, m.RowVersion);
        if (await db.Customers.AnyAsync(x => x.Id != id && x.CustomerNumber == m.CustomerNumber.Trim(), ct))
            throw new CrudConflictException("Ein Kunde mit dieser Kundennummer existiert bereits.");
        e.CustomerNumber = m.CustomerNumber.Trim(); e.Name = m.Name.Trim(); e.LegalName = Trim(m.LegalName);
        e.Email = Trim(m.Email); e.Phone = Trim(m.Phone); e.CountryCode = m.CountryCode.Trim().ToUpperInvariant();
        e.ContactPerson = Trim(m.ContactPerson); e.Street = Trim(m.Street);
        e.HouseNumber = Trim(m.HouseNumber); e.PostalCode = Trim(m.PostalCode); e.City = Trim(m.City);
        e.Type = Parse<CustomerType>(m.Type, nameof(m.Type)); Manual(e);
        await Save(ct); return Result(e);
    }

    public async Task<EntityMutationResult> DeleteCustomerAsync(Guid id, uint version, CancellationToken ct)
    {
        var e = await Customers().Include(x => x.BuildingAssignments).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw Missing("Kunde");
        Concurrency(e, version);
        if (e.BuildingAssignments.Any())
            throw new CrudConflictException($"Der Kunde „{e.Name}“ ist Gebäuden zugeordnet und kann nicht gelöscht werden.");
        Delete(e); await Save(ct); return Result(e);
    }
    public Task<EntityMutationResult> RestoreCustomerAsync(Guid id, uint v, CancellationToken ct) =>
        Restore(Customers(true), id, v, "Kunde", ct);

    public async Task<EntityMutationResult> CreateBuildingAsync(BuildingCreateRequest m, CancellationToken ct)
    {
        Required((nameof(m.Name), m.Name));
        if (m.CustomerId.HasValue && !await Customers().AnyAsync(x => x.Id == m.CustomerId, ct))
            throw Invalid(nameof(m.CustomerId), "Der angegebene Kunde existiert nicht.");
        var e = new Building { BuildingNumber = await _buildingNumbers.NextAsync(ct), Name = m.Name.Trim(),
            ExternalIdentifier = Trim(m.ExternalIdentifier) };
        db.Buildings.Add(e);
        if (HasBuildingVersionData(m))
        {
            var address = await BuildAddress(m, ct);
            e.Versions.Add(new BuildingVersion { VersionNumber = 1, ValidFrom = DateTime.UtcNow,
                GrossFloorAreaM2 = m.GrossFloorAreaM2, YearOfConstruction = m.YearOfConstruction,
                BuildingCategory = ParseOptional(m.BuildingCategory, BuildingCategory.Other, nameof(m.BuildingCategory)),
                PrimaryUseType = ParseOptional(m.PrimaryUseType, PrimaryUseType.Mixed, nameof(m.PrimaryUseType)),
                HeatedFloorAreaM2 = m.HeatedFloorAreaM2,
                YearOfLastMajorRenovation = m.YearOfLastMajorRenovation, Address = address,
                ChangeReason = "Manuelle Anlage" });
        }
        if (m.CustomerId.HasValue) db.CustomerBuildingAssignments.Add(new CustomerBuildingAssignment {
            CustomerId = m.CustomerId.Value, Building = e, ValidFrom = DateTime.UtcNow, IsPrimary = true });
        await Save(ct);
        await SetBuildingState(e.Id, m.BuildingState, ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> UpdateBuildingAsync(Guid id, BuildingUpdateRequest m, CancellationToken ct)
    {
        Required((nameof(m.Name), m.Name));
        var e = await Buildings().Include(x => x.CustomerAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Gebäude");
        Concurrency(e, m.RowVersion);
        if (m.CustomerId.HasValue && !await Customers().AnyAsync(x => x.Id == m.CustomerId, ct))
            throw Invalid(nameof(m.CustomerId), "Der angegebene Kunde existiert nicht.");
        e.Name = m.Name.Trim();
        e.ExternalIdentifier = Trim(m.ExternalIdentifier); Manual(e);
        var assignment = e.CustomerAssignments.FirstOrDefault(x => x.ValidTo == null && x.IsPrimary);
        if (assignment?.CustomerId != m.CustomerId)
        {
            if (assignment is not null) assignment.ValidTo = DateTime.UtcNow;
            if (m.CustomerId.HasValue)
                db.CustomerBuildingAssignments.Add(new CustomerBuildingAssignment {
                    CustomerId = m.CustomerId.Value, BuildingId = id,
                    ValidFrom = DateTime.UtcNow, IsPrimary = true });
        }
        var previous = await db.BuildingVersions.AsNoTracking().Where(x => x.BuildingId == id)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(ct);
        var address = await BuildAddress(m, ct);
        db.BuildingVersions.Add(new BuildingVersion { BuildingId = id,
            VersionNumber = (previous?.VersionNumber ?? 0) + 1, ValidFrom = DateTime.UtcNow,
            GrossFloorAreaM2 = m.GrossFloorAreaM2, YearOfConstruction = m.YearOfConstruction,
            BuildingCategory = ParseOptional(m.BuildingCategory, BuildingCategory.Other, nameof(m.BuildingCategory)),
            PrimaryUseType = ParseOptional(m.PrimaryUseType, PrimaryUseType.Mixed, nameof(m.PrimaryUseType)),
            HeatedFloorAreaM2 = m.HeatedFloorAreaM2,
            YearOfLastMajorRenovation = m.YearOfLastMajorRenovation, Address = address,
            ChangeReason = "Manuelle Änderung" });
        await Save(ct);
        await SetBuildingState(e.Id, m.BuildingState, ct);
        await _qualityInvalidation.InvalidateBuildingConfirmations(
            id, "Gebäudestammdaten wurden geändert.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> DeleteBuildingAsync(Guid id, uint v, CancellationToken ct)
    {
        var e = await Buildings().Include(x => x.Meters).Include(x => x.EnergySystemAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Gebäude");
        Concurrency(e, v);
        if (e.Meters.Count != 0 || e.EnergySystemAssignments.Count != 0)
            throw new CrudConflictException($"Das Gebäude „{e.Name}“ ist mit {e.Meters.Count} Zählpunkten und {e.EnergySystemAssignments.Count} Anlagen verknüpft.");
        Delete(e); await Save(ct); return Result(e);
    }
    public Task<EntityMutationResult> RestoreBuildingAsync(Guid id, uint v, CancellationToken ct) =>
        Restore(Buildings(true), id, v, "Gebäude", ct);

    public async Task<EntityMutationResult> CreateMeterAsync(MeterWriteModel m, CancellationToken ct)
    {
        Required((nameof(m.MeterNumber), m.MeterNumber), (nameof(m.Name), m.Name));
        if (!await Buildings().AnyAsync(x => x.Id == m.BuildingId, ct))
            throw Invalid(nameof(m.BuildingId), "Das angegebene Gebäude existiert nicht.");
        if (await db.Meters.AnyAsync(x => x.MeterNumber == m.MeterNumber.Trim(), ct))
            throw new CrudConflictException("Ein Zählpunkt mit dieser Kennung existiert bereits.");
        if (m.EnergySystemId.HasValue && !await db.EnergySystems.AnyAsync(x => x.Id == m.EnergySystemId, ct))
            throw Invalid(nameof(m.EnergySystemId), "Die angegebene Anlage existiert nicht.");
        ValidateMeterConsistency(m);
        var e = new Meter { MeterNumber = m.MeterNumber.Trim(), Name = m.Name.Trim(), BuildingId = m.BuildingId,
            EnergySystemId = m.EnergySystemId, Medium = Parse<MeterMedium>(m.Medium, nameof(m.Medium)),
            Quantity = Parse<MeterQuantity>(m.Quantity, nameof(m.Quantity)), Unit = Parse<MeterUnit>(m.Unit, nameof(m.Unit)),
            Direction = Parse<MeterDirection>(m.Direction, nameof(m.Direction)), Type = Parse<MeterType>(m.Type, nameof(m.Type)),
            Description = Trim(m.Description), ExternalIdentifier = Trim(m.ExternalIdentifier),
            AnnualValue = m.AnnualValue, AnnualValueOrigin = m.AnnualValue.HasValue ? "Manual" : null };
        db.Meters.Add(e); await Save(ct);
        await _qualityInvalidation.InvalidateBuildingInventory(
            m.BuildingId, "Ein neuer Zählpunkt wurde angelegt.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> UpdateMeterAsync(Guid id, MeterWriteModel m, CancellationToken ct)
    {
        var e = await Meters().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Zählpunkt");
        Concurrency(e, m.RowVersion);
        if (e.BuildingId != m.BuildingId && await db.MeterReadings.AnyAsync(x => x.MeterId == id, ct))
            throw new CrudConflictException("Die Gebäudezuordnung eines Zählpunkts mit Messwerten kann nicht geändert werden.");
        if (!await Buildings().AnyAsync(x => x.Id == m.BuildingId, ct))
            throw Invalid(nameof(m.BuildingId), "Das angegebene Gebäude existiert nicht.");
        ValidateMeterConsistency(m);
        e.Name = m.Name.Trim(); e.BuildingId = m.BuildingId; e.EnergySystemId = m.EnergySystemId;
        e.Medium = Parse<MeterMedium>(m.Medium, nameof(m.Medium)); e.Quantity = Parse<MeterQuantity>(m.Quantity, nameof(m.Quantity));
        e.Unit = Parse<MeterUnit>(m.Unit, nameof(m.Unit)); e.Direction = Parse<MeterDirection>(m.Direction, nameof(m.Direction));
        e.Type = Parse<MeterType>(m.Type, nameof(m.Type)); e.Description = Trim(m.Description);
        e.ExternalIdentifier = Trim(m.ExternalIdentifier); e.AnnualValue = m.AnnualValue;
        e.AnnualValueOrigin = m.AnnualValue.HasValue ? "Manual" : null;
        Manual(e); await Save(ct);
        await _qualityInvalidation.InvalidateMeterAnalysis(
            id, "Zählpunktstammdaten wurden geändert.", ct);
        await _qualityInvalidation.InvalidateBuildingInventory(
            m.BuildingId, "Ein Zählpunkt wurde geändert.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> DeleteMeterAsync(Guid id, uint v, CancellationToken ct)
    {
        var e = await Meters().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Zählpunkt");
        Concurrency(e, v);
        if (await db.MeterReadings.AnyAsync(x => x.MeterId == id, ct))
            throw new CrudConflictException("Der Zählpunkt besitzt Messwerte und kann nur fachlich deaktiviert werden.");
        var buildingId = e.BuildingId;
        Delete(e); e.IsActive = false; await Save(ct);
        await _qualityInvalidation.InvalidateMeterAnalysis(
            id, "Der Zählpunkt wurde entfernt.", ct);
        if (buildingId.HasValue)
            await _qualityInvalidation.InvalidateBuildingInventory(
                buildingId.Value, "Ein Zählpunkt wurde entfernt.", ct);
        return Result(e);
    }
    public Task<EntityMutationResult> RestoreMeterAsync(Guid id, uint v, CancellationToken ct) =>
        Restore(Meters(true), id, v, "Zählpunkt", ct);

    public async Task<EntityMutationResult> CreateEnergySystemAsync(EnergySystemWriteModel m, CancellationToken ct)
    {
        Required((nameof(m.EnergySystemNumber), m.EnergySystemNumber), (nameof(m.Name), m.Name));
        if (!await Buildings().AnyAsync(x => x.Id == m.BuildingId, ct))
            throw Invalid(nameof(m.BuildingId), "Das angegebene Gebäude existiert nicht.");
        if (await db.EnergySystems.AnyAsync(x => x.EnergySystemNumber == m.EnergySystemNumber.Trim(), ct))
            throw new CrudConflictException("Eine Anlage mit dieser Nummer existiert bereits.");
        var e = new EnergySystem { EnergySystemNumber = m.EnergySystemNumber.Trim(), Name = m.Name.Trim(),
            Type = Parse<EnergySystemType>(m.Type, nameof(m.Type)), RatedPowerKw = m.RatedPowerKw,
            CommissionedAt = m.CommissionedAt, DecommissionedAt = m.DecommissionedAt };
        db.EnergySystems.Add(e); db.EnergySystemBuildingAssignments.Add(new EnergySystemBuildingAssignment
            { EnergySystem = e, BuildingId = m.BuildingId, Role = EnergySystemBuildingRole.LocatedAt });
        await Save(ct);
        await _qualityInvalidation.InvalidateBuildingInventory(
            m.BuildingId, "Eine neue Anlage wurde angelegt.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> UpdateEnergySystemAsync(Guid id, EnergySystemWriteModel m, CancellationToken ct)
    {
        var e = await EnergySystems().Include(x => x.BuildingAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Anlage");
        Concurrency(e, m.RowVersion); Required((nameof(m.Name), m.Name));
        if (!await Buildings().AnyAsync(x => x.Id == m.BuildingId, ct))
            throw Invalid(nameof(m.BuildingId), "Das angegebene Gebäude existiert nicht.");
        var assignment = e.BuildingAssignments.FirstOrDefault(x => x.ValidTo == null);
        if (assignment is not null && assignment.BuildingId != m.BuildingId)
        {
            if (await db.Meters.AnyAsync(x => x.EnergySystemId == id, ct))
                throw new CrudConflictException("Die Gebäudezuordnung einer Anlage mit Zählpunkten kann nicht geändert werden.");
            assignment.ValidTo = DateTime.UtcNow;
            db.EnergySystemBuildingAssignments.Add(new EnergySystemBuildingAssignment
                { EnergySystemId = id, BuildingId = m.BuildingId, Role = EnergySystemBuildingRole.LocatedAt });
        }
        e.Name = m.Name.Trim(); e.Type = Parse<EnergySystemType>(m.Type, nameof(m.Type));
        e.RatedPowerKw = m.RatedPowerKw; e.CommissionedAt = m.CommissionedAt;
        e.DecommissionedAt = m.DecommissionedAt; Manual(e);
        await Save(ct);
        await _qualityInvalidation.InvalidateBuildingInventory(
            m.BuildingId, "Anlagendaten oder Zuordnung wurden geändert.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> DeleteEnergySystemAsync(Guid id, uint v, CancellationToken ct)
    {
        var e = await EnergySystems().Include(x => x.Meters).Include(x => x.BuildingAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Anlage");
        Concurrency(e, v);
        if (e.Meters.Count != 0) throw new CrudConflictException("Die Anlage ist Zählpunkten zugeordnet und kann nicht gelöscht werden.");
        var buildingIds = e.BuildingAssignments.Where(x => x.ValidTo == null)
            .Select(x => x.BuildingId).Distinct().ToArray();
        Delete(e); await Save(ct);
        foreach (var buildingId in buildingIds)
            await _qualityInvalidation.InvalidateBuildingInventory(
                buildingId, "Eine Anlage wurde entfernt.", ct);
        return Result(e);
    }
    public Task<EntityMutationResult> RestoreEnergySystemAsync(Guid id, uint v, CancellationToken ct) =>
        Restore(EnergySystems(true), id, v, "Anlage", ct);

    public async Task<EntityMutationResult> CreateMeterReadingAsync(MeterReadingWriteModel m, CancellationToken ct)
    {
        var meter = await Meters().SingleOrDefaultAsync(x => x.Id == m.MeterId, ct) ?? throw Missing("Zählpunkt");
        if (m.Timestamp == default) throw Invalid(nameof(m.Timestamp), "Ein Zeitstempel ist erforderlich.");
        if (await db.MeterReadings.AnyAsync(x => x.MeterId == m.MeterId && x.Timestamp == m.Timestamp, ct))
            throw new CrudConflictException("Für diesen Zählpunkt existiert bereits ein Messwert zum gleichen Zeitpunkt.");
        var e = new MeterReading { MeterId = meter.Id, Timestamp = m.Timestamp.ToUniversalTime(), Value = m.Value,
            ReadingType = Parse<MeterReadingType>(m.ReadingType, nameof(m.ReadingType)),
            QualityFlag = Parse<DataQuality>(m.QualityFlag, nameof(m.QualityFlag)), IntervalSeconds = m.IntervalSeconds };
        db.MeterReadings.Add(e); await Save(ct);
        await _qualityInvalidation.InvalidateMeterAnalysis(
            m.MeterId, "Neue Messwerte wurden gespeichert.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> UpdateMeterReadingAsync(Guid id, MeterReadingWriteModel m, CancellationToken ct)
    {
        var e = await MeterReadings().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Messwert");
        Concurrency(e, m.RowVersion);
        if (e.ReadingType != Parse<MeterReadingType>(m.ReadingType, nameof(m.ReadingType)))
            throw new CrudConflictException("Der Messwerttyp kann nicht stillschweigend geändert werden.");
        e.Value = m.Value; e.QualityFlag = Parse<DataQuality>(m.QualityFlag, nameof(m.QualityFlag)); Manual(e);
        await Save(ct);
        await _qualityInvalidation.InvalidateMeterAnalysis(
            e.MeterId, "Ein Messwert wurde geändert.", ct);
        return Result(e);
    }
    public async Task<EntityMutationResult> DeleteMeterReadingAsync(Guid id, uint v, string? reason, CancellationToken ct)
    {
        var e = await MeterReadings().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing("Messwert");
        Concurrency(e, v); Delete(e); await Save(ct);
        await _qualityInvalidation.InvalidateMeterAnalysis(
            e.MeterId, "Ein Messwert wurde entfernt.", ct);
        return Result(e);
    }
    public async Task<Enset.Application.ReadModel.PagedResult<EnergySystemDto>> GetEnergySystemsAsync(
        EntityListQuery request, CancellationToken ct)
    {
        var query = EnergySystems(request.IncludeDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Search.Trim()}%") ||
                                     EF.Functions.ILike(x.EnergySystemNumber, $"%{request.Search.Trim()}%"));
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page); var size = Math.Clamp(request.PageSize, 1, 200);
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * size).Take(size)
            .Select(x => new EnergySystemDto(x.Id, x.EnergySystemNumber, x.Name, x.Type.ToString(),
                x.BuildingAssignments.OrderBy(a => a.ValidFrom).Select(a => a.BuildingId).FirstOrDefault(),
                x.RatedPowerKw, x.CommissionedAt, x.DecommissionedAt,
                x.IsActive, x.DataOrigin.ToString(), x.CreatedAt, x.UpdatedAt, x.IsDeleted, x.RowVersion)).ToListAsync(ct);
        return new(items, page, size, total);
    }
    public Task<EnergySystemDto?> GetEnergySystemAsync(Guid id, bool includeDeleted, CancellationToken ct)
    {
        var query = EnergySystems(includeDeleted);
        return query.AsNoTracking().Where(x => x.Id == id).Select(x => new EnergySystemDto(x.Id,
            x.EnergySystemNumber, x.Name, x.Type.ToString(),
            x.BuildingAssignments.OrderBy(a => a.ValidFrom).Select(a => a.BuildingId).FirstOrDefault(),
            x.RatedPowerKw, x.CommissionedAt, x.DecommissionedAt,
            x.IsActive, x.DataOrigin.ToString(), x.CreatedAt, x.UpdatedAt, x.IsDeleted, x.RowVersion))
            .SingleOrDefaultAsync(ct);
    }
    public async Task<Enset.Application.ReadModel.PagedResult<MeterReadingDto>> GetMeterReadingsAsync(
        Guid? meterId, EntityListQuery request, CancellationToken ct)
    {
        var query = MeterReadings(request.IncludeDeleted);
        if (meterId.HasValue) query = query.Where(x => x.MeterId == meterId);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page); var size = Math.Clamp(request.PageSize, 1, 200);
        var items = await query.AsNoTracking().OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * size).Take(size).Select(x => new MeterReadingDto(x.Id, x.MeterId,
                x.Timestamp, x.Value, x.ReadingType.ToString(), x.QualityFlag.ToString(),
                x.IntervalSeconds, x.DataOrigin.ToString(), x.IsDeleted, x.RowVersion)).ToListAsync(ct);
        return new(items, page, size, total);
    }
    public Task<MeterReadingDto?> GetMeterReadingAsync(Guid id, bool includeDeleted, CancellationToken ct)
    {
        var query = MeterReadings(includeDeleted);
        return query.AsNoTracking().Where(x => x.Id == id).Select(x => new MeterReadingDto(x.Id,
            x.MeterId, x.Timestamp, x.Value, x.ReadingType.ToString(), x.QualityFlag.ToString(),
            x.IntervalSeconds, x.DataOrigin.ToString(), x.IsDeleted, x.RowVersion)).SingleOrDefaultAsync(ct);
    }
    public async Task<IReadOnlyList<AuditHistoryItem>> GetAuditHistoryAsync(string type, Guid id, CancellationToken ct)
    {
        var visible = type switch
        {
            nameof(Customer) => await Customers(true).AnyAsync(x => x.Id == id, ct),
            nameof(Building) => await Buildings(true).AnyAsync(x => x.Id == id, ct),
            nameof(Meter) => await Meters(true).AnyAsync(x => x.Id == id, ct),
            nameof(MeterReading) => await MeterReadings(true).AnyAsync(x => x.Id == id, ct),
            nameof(EnergySystem) => await EnergySystems(true).AnyAsync(x => x.Id == id, ct),
            _ => false
        };
        if (!visible) throw Missing("Entität");
        var entries = await db.EntityAuditEntries.AsNoTracking().Where(x => x.EntityType == type && x.EntityId == id)
            .OrderBy(x => x.ChangedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
        var userIds = entries.Where(x => x.Source == LastModifiedSource.User)
            .Select(x => x.ChangedByUserId).Distinct().ToArray();
        var names = await db.ApplicationUsers.AsNoTracking()
            .Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        return entries.Select(x => new AuditHistoryItem(x.ChangedAtUtc, x.ChangedByUserId, x.ChangeType.ToString(),
            x.FieldName, x.OldValue, x.NewValue, x.Source.ToString(), x.ImportId, x.Reason)
        {
            ChangedByDisplayName = x.Source == LastModifiedSource.System ? "ENSET-System"
                : x.Source == LastModifiedSource.Import ? "Import"
                : x.DisplayNameSnapshot ?? names.GetValueOrDefault(x.ChangedByUserId) ?? "Unbekannt"
        }).ToArray();
    }

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new CrudConflictException("Der Datensatz wurde zwischenzeitlich geändert. Laden Sie die aktuellen Daten neu."); }
        catch (DbUpdateException) { throw new CrudConflictException("Die Änderung verletzt eine Eindeutigkeits- oder Abhängigkeitsregel."); }
    }
    private async Task<EntityMutationResult> Restore<T>(IQueryable<T> query, Guid id, uint v, string label, CancellationToken ct) where T : BaseEntity
    { var e = await query.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing(label); Concurrency(e, v);
      e.IsDeleted = false; e.DeletedAtUtc = null; e.DeletedByUserId = null; Manual(e); await Save(ct); return Result(e); }
    private static void Required(params (string Field, string Value)[] values)
    { var errors = values.Where(x => string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Field, _ => new[] { "Dieses Feld ist erforderlich." });
      if (errors.Count != 0) throw new CrudValidationException(errors); }
    private static T Parse<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) ? parsed : throw Invalid(field, "Der angegebene Wert ist ungültig.");
    private static T ParseOptional<T>(string? value, T fallback, string field) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? fallback : Parse<T>(value, field);
    private static CrudValidationException Invalid(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
    private static CrudNotFoundException Missing(string label) => new($"{label} wurde nicht gefunden.");
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool HasBuildingVersionData(IBuildingMutationModel m) =>
        m.GrossFloorAreaM2.HasValue || m.YearOfConstruction.HasValue ||
        m.HeatedFloorAreaM2.HasValue || m.YearOfLastMajorRenovation.HasValue ||
        !string.IsNullOrWhiteSpace(m.BuildingCategory) || !string.IsNullOrWhiteSpace(m.PrimaryUseType) ||
        !string.IsNullOrWhiteSpace(m.PostalCode) || !string.IsNullOrWhiteSpace(m.City) ||
        !string.IsNullOrWhiteSpace(m.Street) || !string.IsNullOrWhiteSpace(m.HouseNumber);
    private async Task<Address?> BuildAddress(IBuildingMutationModel m, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(m.PostalCode) && string.IsNullOrWhiteSpace(m.City) &&
            string.IsNullOrWhiteSpace(m.Street) && string.IsNullOrWhiteSpace(m.HouseNumber))
            return null;
        var country = await db.Countries.SingleOrDefaultAsync(x => x.IsoCode2 == "AT", ct)
            ?? throw Invalid(nameof(m.PostalCode), "Das Referenzland AT ist nicht eingerichtet.");
        PostalCodeArea? area = null;
        if (!string.IsNullOrWhiteSpace(m.PostalCode))
        {
            var code = m.PostalCode.Trim();
            area = await db.PostalCodeAreas.SingleOrDefaultAsync(
                x => x.CountryId == country.Id && x.Code == code, ct);
            if (area is null)
            {
                area = new PostalCodeArea { CountryId = country.Id, Code = code, Name = Trim(m.City) };
                db.PostalCodeAreas.Add(area);
            }
            else if (!string.IsNullOrWhiteSpace(m.City) && string.IsNullOrWhiteSpace(area.Name))
                area.Name = m.City.Trim();
        }
        return new Address { CountryId = country.Id, PostalCodeArea = area,
            Street = Trim(m.Street), HouseNumber = Trim(m.HouseNumber) };
    }
    private async Task SetBuildingState(Guid buildingId, string? value, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var current = await db.CuratedFieldValues.Where(x => x.EntityType == "Building" &&
            x.EntityId == buildingId &&
            x.FieldName == "BuildingState" &&
            x.ValidToUtc == null)
            .ToListAsync(ct);
        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var old in current) old.ValidToUtc = now;
            if (current.Count > 0) await Save(ct);
            return;
        }
        var state = Parse<BuildingState>(value, nameof(IBuildingMutationModel.BuildingState));
        if (current.Count == 1 && current[0].NormalizedValue == state.ToString())
            return;
        foreach (var old in current) old.ValidToUtc = now;
        db.CuratedFieldValues.Add(new CuratedFieldValue { EntityType = "Building",
            EntityId = buildingId, FieldName = "BuildingState",
            OriginalValue = current.FirstOrDefault()?.CuratedValue,
            CuratedValue = state.ToString(), NormalizedValue = state.ToString(),
            Source = CurationSource.User, MaturityLevel = DataMaturityLevel.Silver,
            ConfidencePercent = 100, Confirmed = false,
            ValidFromUtc = now });
        await Save(ct);
    }
    private void Concurrency(BaseEntity e, uint version)
    {
        if (version == 0) throw new CrudConflictException("Ein Concurrency-Token ist erforderlich.");
        db.Entry(e).Property(x => x.RowVersion).OriginalValue = version;
    }
    private static void Manual(BaseEntity e) => e.LastModifiedSource = LastModifiedSource.User;
    private static void Delete(BaseEntity e) { e.IsDeleted = true; e.DeletedAtUtc = DateTime.UtcNow; Manual(e); }
    private static void ValidateMeterConsistency(MeterWriteModel m)
    {
        var quantity = Parse<MeterQuantity>(m.Quantity, nameof(m.Quantity));
        var unit = Parse<MeterUnit>(m.Unit, nameof(m.Unit));
        var valid = quantity switch
        {
            MeterQuantity.Energy => unit is MeterUnit.Wh or MeterUnit.KWh or MeterUnit.MWh,
            MeterQuantity.Power => unit is MeterUnit.W or MeterUnit.KW or MeterUnit.MW,
            MeterQuantity.Volume => unit is MeterUnit.CubicMeter or MeterUnit.Liter,
            MeterQuantity.Flow => unit is MeterUnit.CubicMeterPerHour or MeterUnit.LiterPerSecond,
            MeterQuantity.Temperature => unit is MeterUnit.Celsius or MeterUnit.Kelvin,
            _ => unit != MeterUnit.Unknown
        };
        if (!valid) throw Invalid(nameof(m.Unit), "Die Einheit ist für die gewählte Messgröße nicht zulässig.");
    }
    private static EntityMutationResult Result(BaseEntity e) => new(e.Id, e.RowVersion, e.DataOrigin.ToString(),
        e.CreatedAt, e.CreatedByUserId, e.UpdatedAt, e.UpdatedByUserId, e.IsDeleted);
    private IQueryable<Customer> Customers(bool deleted = false)
    {
        IQueryable<Customer> query = deleted ? db.Customers.IgnoreQueryFilters() : db.Customers;
        return scope?.ApplyCustomerScope(query) ?? query;
    }
    private IQueryable<Building> Buildings(bool deleted = false)
    {
        IQueryable<Building> query = deleted ? db.Buildings.IgnoreQueryFilters() : db.Buildings;
        return scope?.ApplyBuildingScope(query) ?? query;
    }
    private IQueryable<Meter> Meters(bool deleted = false)
    {
        IQueryable<Meter> query = deleted ? db.Meters.IgnoreQueryFilters() : db.Meters;
        return scope?.ApplyMeterScope(query) ?? query;
    }
    private IQueryable<MeterReading> MeterReadings(bool deleted = false)
    {
        IQueryable<MeterReading> query = deleted ? db.MeterReadings.IgnoreQueryFilters() : db.MeterReadings;
        return scope?.ApplyMeterReadingScope(query) ?? query;
    }
    private IQueryable<EnergySystem> EnergySystems(bool deleted = false)
    {
        IQueryable<EnergySystem> query = deleted ? db.EnergySystems.IgnoreQueryFilters() : db.EnergySystems;
        if (scope is null) return query;
        var buildings = scope.ApplyBuildingScope(db.Buildings).Select(x => x.Id);
        return query.Where(x => x.BuildingAssignments.Any(a => buildings.Contains(a.BuildingId)));
    }
}
