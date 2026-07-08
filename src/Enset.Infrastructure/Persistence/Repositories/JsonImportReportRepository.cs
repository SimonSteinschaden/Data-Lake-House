using System.Text.Json;
using System.Text.Json.Serialization;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Reports;

namespace Enset.Infrastructure.Imports.Persistence;

public sealed class JsonImportReportRepository : IImportReportRepository
{
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonImportReportRepository(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task SaveAsync(
        ImportReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var destinationPath = GetReportPath(report.ImportId);
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    report,
                    _serializerOptions,
                    cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            _lock.Release();
        }
    }

    public async Task<ImportReport?> GetAsync(
        Guid importId,
        CancellationToken cancellationToken = default)
    {
        var reportPath = GetReportPath(importId);
        if (!File.Exists(reportPath))
            return null;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var stream = new FileStream(
                reportPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous);

            return await JsonSerializer.DeserializeAsync<ImportReport>(
                stream,
                _serializerOptions,
                cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetReportPath(Guid importId)
    {
        if (importId == Guid.Empty)
            throw new ArgumentException("Import id must not be empty.", nameof(importId));

        return Path.Combine(_rootPath, $"{importId:N}.json");
    }
}
