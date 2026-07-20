using Enset.Api.Controllers;
using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Domain.DataProducts;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Enset.Import.Tests;

public class DataProductsControllerTests
{
    [Fact]
    public async Task List_returns_api_dtos()
    {
        var product = new DataProduct { Name = "Profile", Definition = new DataProductDefinition { Code = "BUILDING_ENERGY_PROFILE" } };
        var controller = Controller(new Repository(product));
        var result = await controller.List(default);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Latest_returns_problem_details_when_no_version_exists()
    {
        var controller = Controller(new Repository(new DataProduct { Definition = new DataProductDefinition() }));
        var result = await controller.Latest(Guid.NewGuid(), default);
        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(404, problem.StatusCode);
    }

    private static DataProductsController Controller(IDataProductRepository repository) =>
        new(repository, new Availability(), new Generation());

    private sealed class Availability : IDataProductGenerationAvailabilityService { public Task<DataProductGenerationAvailability> CheckAsync(GenerateDataProductCommand command, CancellationToken cancellationToken = default) => Task.FromResult(DataProductGenerationAvailability.Available()); }
    private sealed class Generation : IDataProductGenerationService { public Task<DataProductVersion> GenerateAsync(GenerateDataProductCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new DataProductVersion()); }
    private sealed class Repository(DataProduct product) : IDataProductRepository { public Task<DataProduct?> GetForGenerationAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DataProduct?>(product); public Task<IReadOnlyList<DataProduct>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DataProduct>>([product]); public Task<DataProductVersion?> GetLatestVersionAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<DataProductVersion?>(null); public Task<IReadOnlyList<DataProductVersion>> GetVersionsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DataProductVersion>>([]); public Task<int> GetNextVersionNumberAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(1); public Task AddVersionAsync(DataProductVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
