using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Abstractions;

public interface IImportAnalysisService
{
    Task<ImportReport> AnalyzeAsync(
        Stream source,
        string fileName,
        string? contentType,
        string userId,
        CancellationToken cancellationToken = default,
        ImportSourceType sourceType = ImportSourceType.CRM_Excel,
        ImportMedium? medium = null,
        string? defaultMeterNumber = null);
}
