using FluentValidation;

namespace Enset.Application.Crud;

public sealed record CreateCustomerCommand(CustomerWriteModel Model);
public sealed record UpdateCustomerCommand(Guid Id, CustomerWriteModel Model);
public sealed record DeleteCustomerCommand(Guid Id, uint RowVersion);
public sealed record RestoreCustomerCommand(Guid Id, uint RowVersion);
public sealed record CreateBuildingCommand(BuildingWriteModel Model);
public sealed record UpdateBuildingCommand(Guid Id, BuildingWriteModel Model);
public sealed record DeleteBuildingCommand(Guid Id, uint RowVersion);
public sealed record RestoreBuildingCommand(Guid Id, uint RowVersion);
public sealed record CreateMeteringPointCommand(MeterWriteModel Model);
public sealed record UpdateMeteringPointCommand(Guid Id, MeterWriteModel Model);
public sealed record DeleteMeteringPointCommand(Guid Id, uint RowVersion);
public sealed record RestoreMeteringPointCommand(Guid Id, uint RowVersion);
public sealed record CreateEnergySystemCommand(EnergySystemWriteModel Model);
public sealed record UpdateEnergySystemCommand(Guid Id, EnergySystemWriteModel Model);
public sealed record DeleteEnergySystemCommand(Guid Id, uint RowVersion);
public sealed record RestoreEnergySystemCommand(Guid Id, uint RowVersion);
public sealed record CreateMeterReadingCommand(MeterReadingWriteModel Model);
public sealed record UpdateMeterReadingCommand(Guid Id, MeterReadingWriteModel Model);
public sealed record DeleteMeterReadingCommand(Guid Id, uint RowVersion, string? Reason);

public sealed record GetCustomerByIdQuery(Guid Id);
public sealed record GetCustomersQuery(int Page, int PageSize, string? Search);
public sealed record GetBuildingByIdQuery(Guid Id);
public sealed record GetBuildingsQuery(int Page, int PageSize, string? Search);
public sealed record GetMeteringPointByIdQuery(Guid Id);
public sealed record GetMeteringPointsQuery(int Page, int PageSize, string? Search);
public sealed record GetEnergySystemByIdQuery(Guid Id, bool IncludeDeleted = false);
public sealed record GetEnergySystemsQuery(int Page, int PageSize, string? Search, bool IncludeDeleted = false);
public sealed record GetMeterReadingByIdQuery(Guid Id, bool IncludeDeleted = false);
public sealed record GetMeterReadingsQuery(int Page, int PageSize, Guid? MeteringPointId, bool IncludeDeleted = false);
public sealed record GetAuditHistoryQuery(string EntityType, Guid EntityId, int Page = 1, int PageSize = 100);

public sealed class CustomerWriteModelValidator : AbstractValidator<CustomerWriteModel>
{
    public CustomerWriteModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CustomerNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}
public sealed class BuildingWriteModelValidator : AbstractValidator<BuildingWriteModel>
{
    public BuildingWriteModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.BuildingNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.GrossFloorAreaM2).GreaterThanOrEqualTo(0).When(x => x.GrossFloorAreaM2.HasValue);
        RuleFor(x => x.YearOfConstruction).InclusiveBetween(1700, DateTime.UtcNow.Year + 1).When(x => x.YearOfConstruction.HasValue);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}
public sealed class MeterWriteModelValidator : AbstractValidator<MeterWriteModel>
{
    public MeterWriteModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.MeterNumber).NotEmpty().MaximumLength(128);
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.Medium).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}
public sealed class EnergySystemWriteModelValidator : AbstractValidator<EnergySystemWriteModel>
{
    public EnergySystemWriteModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.EnergySystemNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.RatedPowerKw).GreaterThanOrEqualTo(0).When(x => x.RatedPowerKw.HasValue);
        RuleFor(x => x.DecommissionedAt).GreaterThanOrEqualTo(x => x.CommissionedAt)
            .When(x => x.CommissionedAt.HasValue && x.DecommissionedAt.HasValue);
    }
}
public sealed class MeterReadingWriteModelValidator : AbstractValidator<MeterReadingWriteModel>
{
    public MeterReadingWriteModelValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.Timestamp).NotEmpty();
        RuleFor(x => x.ReadingType).NotEmpty();
        RuleFor(x => x.QualityFlag).NotEmpty();
        RuleFor(x => x.IntervalSeconds).GreaterThan(0).When(x => x.IntervalSeconds.HasValue);
    }
}
