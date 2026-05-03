using Enset.Application.Imports.Enums;

namespace Enset.Infrastructure.Imports.Services;

public interface IImportService
{
    Task ImportMeterReadingsAsync(
        Stream inputStream,
        ImportSourceType sourceType,
        Guid importJobId,
        CancellationToken cancellationToken = default);
}