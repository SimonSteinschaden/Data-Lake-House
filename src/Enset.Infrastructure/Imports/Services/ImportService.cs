using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;

namespace Enset.Infrastructure.Imports.Services;

public class ImportService : IImportService
{
    private readonly IMeterReadingReaderFactory _readerFactory;

    public ImportService(IMeterReadingReaderFactory readerFactory)
    {
        _readerFactory = readerFactory;
    }

    public async Task ImportMeterReadingsAsync(
        Stream inputStream,
        ImportSourceType sourceType,
        Guid importJobId,
        CancellationToken cancellationToken = default)
    {
        var reader = _readerFactory.Create(sourceType);

        var dtos = reader.Read(inputStream);

        foreach (var dto in dtos)
        {
            // 1. validieren
            // 2. mappen
            // 3. speichern
        }

        await Task.CompletedTask;
    }
}