using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Models;
using Enset.Application.Exports.LEB.Validation;

namespace Enset.Application.Exports.LEB.Abstractions;

public interface INoeLebContractBuilder
{
    Task<LebExportDataset> BuildAsync(LebExportRequest request, CancellationToken ct);
}
public interface ICsvLebExporter
{
    LebExportFile Export(NoeLebExportContractV1 contract);
}
public interface IExcelLebExporter
{
    LebExportFile Export(NoeLebExportContractV1 contract);
}
public interface ILebExportService
{
    Task<ValidationResult> ValidateAsync(LebExportRequest request, CancellationToken ct);
    Task<LebExportFile> ExportCsvAsync(LebExportRequest request, CancellationToken ct);
    Task<LebExportFile> ExportExcelAsync(LebExportRequest request, CancellationToken ct);
}
public sealed class LebExportValidationException(ValidationResult validation)
    : Exception("Der LEB-Export enthält blockierende Validierungsfehler.")
{
    public ValidationResult Validation { get; } = validation;
}
