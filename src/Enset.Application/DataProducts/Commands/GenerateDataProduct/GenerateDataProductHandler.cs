using Enset.Application.DataProducts.Generation.Abstractions;

namespace Enset.Application.DataProducts.Commands.GenerateDataProduct;

/// <summary>
/// Übergibt einen Generierungsbefehl an den zuständigen Application Service.
/// </summary>
public sealed class GenerateDataProductHandler
{
    private readonly IDataProductGenerationService _generationService;

    public GenerateDataProductHandler(
        IDataProductGenerationService generationService)
    {
        _generationService = generationService;
    }

    public Task HandleAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        return _generationService.GenerateAsync(
            command,
            cancellationToken);
    }
}
