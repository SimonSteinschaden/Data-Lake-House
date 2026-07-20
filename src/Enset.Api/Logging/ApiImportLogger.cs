using Enset.Application.Imports.Abstractions;

namespace Enset.Api.Logging;

public sealed class ApiImportLogger : IImportLogger
{
    private readonly ILogger<ApiImportLogger> _logger;

    public ApiImportLogger(ILogger<ApiImportLogger> logger)
    {
        _logger = logger;
    }

    public void Info(string message) => _logger.LogInformation("{Message}", message);

    public void Warning(string message) => _logger.LogWarning("{Message}", message);

    public void Error(string message) => _logger.LogError("{Message}", message);

    public void Error(string message, Exception exception) =>
        _logger.LogError(exception, "{Message}", message);
}
