using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Domain.DataProducts;

namespace Enset.Application.DataProducts.Generation.Services;

public sealed class DataProductGenerationService
    : IDataProductGenerationService
{
    private readonly IDataProductRepository _dataProductRepository;
    private readonly IDataProductGenerationRunRepository _generationRunRepository;
    private readonly IDataProductGenerationAuthorizationService _authorizationService;
    private readonly IDataProductGenerationAvailabilityService _availabilityService;
    private readonly IReadOnlyDictionary<string, IDataProductGenerator> _generators;

    public DataProductGenerationService(
        IDataProductRepository dataProductRepository,
        IDataProductGenerationRunRepository generationRunRepository,
        IDataProductGenerationAuthorizationService authorizationService,
        IDataProductGenerationAvailabilityService availabilityService,
        IEnumerable<IDataProductGenerator> generators)
    {
        _dataProductRepository = dataProductRepository;
        _generationRunRepository = generationRunRepository;
        _authorizationService = authorizationService;
        _availabilityService = availabilityService;

        _generators = generators.ToDictionary(
            generator => generator.DefinitionCode,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task GenerateAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var dataProduct = await _dataProductRepository.GetForGenerationAsync(
            command.DataProductId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Data Product '{command.DataProductId}' wurde nicht gefunden.");

        var authorization = await _authorizationService.AuthorizeAsync(
            command,
            cancellationToken);

        if (!authorization.IsAuthorized)
        {
            throw new UnauthorizedAccessException(
                authorization.DenialReason
                ?? "Die Berechnung ist für diesen Benutzer nicht freigegeben.");
        }

        var availability = await _availabilityService.CheckAsync(
            command,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(
                string.Join("; ", availability.MissingInputs));
        }

        if (!_generators.TryGetValue(
                dataProduct.Definition.Code,
                out var generator))
        {
            throw new InvalidOperationException(
                $"Für '{dataProduct.Definition.Code}' ist kein Generator registriert.");
        }

        var context = new DataProductGenerationContext
        {
            Command = command,
            DataProduct = dataProduct,
            Definition = dataProduct.Definition
        };

        var run = new DataProductGenerationRun
        {
            StartedAt = DateTime.UtcNow,
            Status = DataProductGenerationStatus.Running,
            GeneratorName = generator.GetType().Name,
            GeneratorVersion = "1.0.0",
            TriggeredBy = command.RequestedByUserId.ToString()
        };

        await _generationRunRepository.AddAsync(
            run,
            cancellationToken);

        try
        {
            await generator.GenerateAsync(
                context,
                cancellationToken);
            
            var nextVersionNumber =
            await _dataProductRepository.GetNextVersionNumberAsync(
                dataProduct.Id,
                cancellationToken);

            var version = CreateVersion(
                dataProduct,
                context,
                run,
                nextVersionNumber);

            await _dataProductRepository.AddVersionAsync(
                version,
                cancellationToken);

            run.Status = context.Warnings.Count > 0
                ? DataProductGenerationStatus.CompletedWithWarnings
                : DataProductGenerationStatus.Completed;

            run.CompletedAt = DateTime.UtcNow;

            if (context.Warnings.Count > 0)
            {
                run.Warnings = string.Join(
                    Environment.NewLine,
                    context.Warnings);
            }

            await _generationRunRepository.UpdateAsync(
                run,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            run.Status = DataProductGenerationStatus.Cancelled;
            run.CompletedAt = DateTime.UtcNow;

            await _generationRunRepository.UpdateAsync(
                run,
                CancellationToken.None);

            throw;
        }
        catch (Exception exception)
        {
            run.Status = DataProductGenerationStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = exception.Message;

            await _generationRunRepository.UpdateAsync(
                run,
                CancellationToken.None);

            throw;
        }
    }
  
  
    private static DataProductVersion CreateVersion(
        DataProduct dataProduct,
        DataProductGenerationContext context,
        DataProductGenerationRun run,
        int nextVersionNumber)
    {
        var version = new DataProductVersion
        {
            DataProductId = dataProduct.Id,
            DataProduct = dataProduct,

            VersionNumber = nextVersionNumber,

            Status = DataProductVersionStatus.Generated,
            GeneratedAt = DateTime.UtcNow,

            InputPeriodFrom = context.PeriodFrom,
            InputPeriodTo = context.PeriodTo,

            Quality = context.Quality,

            GenerationRunId = run.Id,
            GenerationRun = run
        };

        foreach (var value in context.Values)
        {
            value.DataProductVersion = version;
            version.Values.Add(value);
        }

        return version;
    }
}