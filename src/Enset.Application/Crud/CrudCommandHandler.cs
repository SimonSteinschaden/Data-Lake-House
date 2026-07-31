using FluentValidation;

namespace Enset.Application.Crud;

public sealed class CrudCommandHandler(
    IEntityCrudService service,
    IValidator<CustomerWriteModel> customerValidator,
    IValidator<BuildingCreateRequest> buildingCreateValidator,
    IValidator<BuildingUpdateRequest> buildingUpdateValidator,
    IValidator<MeterWriteModel> meterValidator,
    IValidator<EnergySystemWriteModel> energySystemValidator,
    IValidator<MeterReadingWriteModel> readingValidator)
{
    public async Task<EntityMutationResult> Handle(CreateCustomerCommand c, CancellationToken ct)
    { await Validate(customerValidator, c.Model, ct); return await service.CreateCustomerAsync(c.Model, ct); }
    public async Task<EntityMutationResult> Handle(UpdateCustomerCommand c, CancellationToken ct)
    { await Validate(customerValidator, c.Model, ct); return await service.UpdateCustomerAsync(c.Id, c.Model, ct); }
    public Task<EntityMutationResult> Handle(DeleteCustomerCommand c, CancellationToken ct) => service.DeleteCustomerAsync(c.Id, c.RowVersion, ct);
    public Task<EntityMutationResult> Handle(RestoreCustomerCommand c, CancellationToken ct) => service.RestoreCustomerAsync(c.Id, c.RowVersion, ct);
    public async Task<EntityMutationResult> Handle(CreateBuildingCommand c, CancellationToken ct)
    { await Validate(buildingCreateValidator, c.Model, ct); return await service.CreateBuildingAsync(c.Model, ct); }
    public async Task<EntityMutationResult> Handle(UpdateBuildingCommand c, CancellationToken ct)
    { await Validate(buildingUpdateValidator, c.Model, ct); return await service.UpdateBuildingAsync(c.Id, c.Model, ct); }
    public Task<EntityMutationResult> Handle(DeleteBuildingCommand c, CancellationToken ct) => service.DeleteBuildingAsync(c.Id, c.RowVersion, ct);
    public Task<EntityMutationResult> Handle(RestoreBuildingCommand c, CancellationToken ct) => service.RestoreBuildingAsync(c.Id, c.RowVersion, ct);
    public async Task<EntityMutationResult> Handle(CreateMeteringPointCommand c, CancellationToken ct)
    { await Validate(meterValidator, c.Model, ct); return await service.CreateMeterAsync(c.Model, ct); }
    public async Task<EntityMutationResult> Handle(UpdateMeteringPointCommand c, CancellationToken ct)
    { await Validate(meterValidator, c.Model, ct); return await service.UpdateMeterAsync(c.Id, c.Model, ct); }
    public Task<EntityMutationResult> Handle(DeleteMeteringPointCommand c, CancellationToken ct) => service.DeleteMeterAsync(c.Id, c.RowVersion, ct);
    public Task<EntityMutationResult> Handle(RestoreMeteringPointCommand c, CancellationToken ct) => service.RestoreMeterAsync(c.Id, c.RowVersion, ct);
    public async Task<EntityMutationResult> Handle(CreateEnergySystemCommand c, CancellationToken ct)
    { await Validate(energySystemValidator, c.Model, ct); return await service.CreateEnergySystemAsync(c.Model, ct); }
    public async Task<EntityMutationResult> Handle(UpdateEnergySystemCommand c, CancellationToken ct)
    { await Validate(energySystemValidator, c.Model, ct); return await service.UpdateEnergySystemAsync(c.Id, c.Model, ct); }
    public Task<EntityMutationResult> Handle(DeleteEnergySystemCommand c, CancellationToken ct) => service.DeleteEnergySystemAsync(c.Id, c.RowVersion, ct);
    public Task<EntityMutationResult> Handle(RestoreEnergySystemCommand c, CancellationToken ct) => service.RestoreEnergySystemAsync(c.Id, c.RowVersion, ct);
    public async Task<EntityMutationResult> Handle(CreateMeterReadingCommand c, CancellationToken ct)
    { await Validate(readingValidator, c.Model, ct); return await service.CreateMeterReadingAsync(c.Model, ct); }
    public async Task<EntityMutationResult> Handle(UpdateMeterReadingCommand c, CancellationToken ct)
    { await Validate(readingValidator, c.Model, ct); return await service.UpdateMeterReadingAsync(c.Id, c.Model, ct); }
    public Task<EntityMutationResult> Handle(DeleteMeterReadingCommand c, CancellationToken ct) => service.DeleteMeterReadingAsync(c.Id, c.RowVersion, c.Reason, ct);

    private static async Task Validate<T>(IValidator<T> validator, T model, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(model, ct);
        if (!result.IsValid)
            throw new CrudValidationException(result.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray()));
    }
}
