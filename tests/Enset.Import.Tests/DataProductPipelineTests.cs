using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Application.DataProducts.Generation.Services;
using Enset.Domain.DataProducts;
using Xunit;

namespace Enset.Import.Tests;

public class DataProductPipelineTests
{
    [Fact]
    public async Task Unauthorized_request_does_not_create_run()
    {
        var repository = new Repository(CreateProduct()); var runs = new RunRepository();
        var service = new DataProductGenerationService(repository, runs,
            new Authorization(false), new Availability(true), [new Generator()]);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GenerateAsync(Command()));
        Assert.Empty(runs.Added);
    }

    [Fact]
    public async Task Missing_input_does_not_create_run()
    {
        var repository = new Repository(CreateProduct()); var runs = new RunRepository();
        var service = new DataProductGenerationService(repository, runs,
            new Authorization(true), new Availability(false), [new Generator()]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(Command()));
        Assert.Empty(runs.Added);
    }

    [Fact]
    public async Task Successful_generation_creates_run_version_and_values()
    {
        var repository = new Repository(CreateProduct()); var runs = new RunRepository();
        var service = new DataProductGenerationService(repository, runs,
            new Authorization(true), new Availability(true), [new Generator()]);
        var version = await service.GenerateAsync(Command());
        Assert.Single(runs.Added); Assert.Single(version.Values);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(DataProductGenerationStatus.Completed, runs.Added[0].Status);
    }

    private static GenerateDataProductCommand Command() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
    private static DataProduct CreateProduct() { var definition = new DataProductDefinition { Code = "TEST" }; var product = new DataProduct { Id = Command().DataProductId, Definition = definition }; product.ScopeAssignments.Add(new DataProductScopeAssignment { ScopeType = DataProductScopeType.Meter }); return product; }

    private sealed class Generator : IDataProductGenerator { public string DefinitionCode => "TEST"; public Task<DataProductGenerationAvailability> CheckInputAvailabilityAsync(DataProductGenerationContext context, CancellationToken cancellationToken = default) => Task.FromResult(DataProductGenerationAvailability.Available()); public Task GenerateAsync(DataProductGenerationContext context, CancellationToken cancellationToken = default) { context.Values.Add(new DataProductValue { Key = "VALUE", NumericValue = 1 }); return Task.CompletedTask; } }
    private sealed class Authorization(bool allowed) : IDataProductGenerationAuthorizationService { public Task<DataProductGenerationAuthorizationResult> AuthorizeAsync(GenerateDataProductCommand command, CancellationToken cancellationToken = default) => Task.FromResult(allowed ? DataProductGenerationAuthorizationResult.Allowed() : DataProductGenerationAuthorizationResult.Denied("denied")); }
    private sealed class Availability(bool available) : IDataProductGenerationAvailabilityService { public Task<DataProductGenerationAvailability> CheckAsync(GenerateDataProductCommand command, CancellationToken cancellationToken = default) => Task.FromResult(available ? DataProductGenerationAvailability.Available() : DataProductGenerationAvailability.MissingData("readings")); }
    private sealed class RunRepository : IDataProductGenerationRunRepository { public List<DataProductGenerationRun> Added { get; } = []; public Task AddAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default) { Added.Add(run); return Task.CompletedTask; } public Task UpdateAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class Repository(DataProduct product) : IDataProductRepository { public Task<DataProduct?> GetForGenerationAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DataProduct?>(product); public Task<int> GetNextVersionNumberAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(1); public Task AddVersionAsync(DataProductVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task<IReadOnlyList<DataProduct>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DataProduct>>([product]); public Task<DataProductVersion?> GetLatestVersionAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DataProductVersion?>(null); public Task<IReadOnlyList<DataProductVersion>> GetVersionsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DataProductVersion>>([]); }
}
