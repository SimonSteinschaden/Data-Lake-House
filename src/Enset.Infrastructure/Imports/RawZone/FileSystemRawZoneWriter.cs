using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.WriteGate;

namespace Enset.Infrastructure.Imports.RawZone;

public sealed class FileSystemRawZoneWriter : IRawZoneWriter
{
    private readonly string _rootPath;

    public FileSystemRawZoneWriter(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> ArchiveAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default)
    {
        var source = context.Report?.SourceFile
            ?? throw new InvalidOperationException("Source file metadata is required.");

        if (string.IsNullOrWhiteSpace(source.StagedPath) || !File.Exists(source.StagedPath))
            throw new FileNotFoundException("The staged source file does not exist.", source.StagedPath);

        var importDirectory = Path.Combine(_rootPath, context.ImportId.ToString("N"));
        Directory.CreateDirectory(importDirectory);

        var safeFileName = Path.GetFileName(source.FileName);
        var hashPrefix = string.IsNullOrWhiteSpace(source.Sha256)
            ? "unverified"
            : source.Sha256.ToLowerInvariant();
        var destinationPath = Path.Combine(
            importDirectory,
            $"{hashPrefix}_{safeFileName}");

        await using var input = File.OpenRead(source.StagedPath);
        await using var output = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await input.CopyToAsync(output, cancellationToken);

        return destinationPath;
    }
}
