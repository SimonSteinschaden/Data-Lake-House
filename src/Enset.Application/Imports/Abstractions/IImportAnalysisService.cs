using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Abstractions;

public interface IImportAnalysisService
{
    Task<ImportReport> AnalyzeAsync(
        Stream source,
        string fileName,
        string? contentType,
        string userId,
        CancellationToken cancellationToken = default);
}
