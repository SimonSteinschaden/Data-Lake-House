using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.WriteGate;

namespace Enset.Infrastructure.Imports.Excel;

public sealed class ExcelImportWriter : IImportWriter
{
    private readonly string? _allowedRootPath;

    public ExcelImportWriter(string? allowedRootPath = null)
    {
        _allowedRootPath = string.IsNullOrWhiteSpace(allowedRootPath)
            ? null
            : Path.GetFullPath(allowedRootPath);

        if (_allowedRootPath is not null)
            Directory.CreateDirectory(_allowedRootPath);
    }

    public ImportWriterType WriterType => ImportWriterType.Excel;

    public Task WriteAsync(
        ImportWriteContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourcePath = context.Report?.SourceFile?.StagedPath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("The staged Excel source file does not exist.", sourcePath);

        if (string.IsNullOrWhiteSpace(context.TargetLocation))
            throw new InvalidOperationException("An Excel target location is required.");

        var targetPath = ResolveTargetPath(context.TargetLocation);
        if (string.Equals(
                Path.GetFullPath(sourcePath),
                targetPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The staged source file cannot be used as the Excel output target.");
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        File.Copy(sourcePath, targetPath, overwrite: true);

        IExcelWorkbookWriter workbookWriter = new ExcelWorkbookWriter(targetPath);
        workbookWriter.UpdateCustomers(context.Customers);

        return Task.CompletedTask;
    }

    private string ResolveTargetPath(string targetLocation)
    {
        if (_allowedRootPath is null)
            return Path.GetFullPath(targetLocation);

        var candidate = Path.IsPathRooted(targetLocation)
            ? Path.GetFullPath(targetLocation)
            : Path.GetFullPath(Path.Combine(_allowedRootPath, targetLocation));

        var relativePath = Path.GetRelativePath(_allowedRootPath, candidate);
        if (relativePath == ".." ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The Excel target location must be inside the configured output directory.");
        }

        return candidate;
    }
}
