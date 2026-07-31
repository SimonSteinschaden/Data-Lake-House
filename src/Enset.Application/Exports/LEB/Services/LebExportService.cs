using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Models;
using Enset.Application.Exports.LEB.Validation;

namespace Enset.Application.Exports.LEB.Services;

public sealed class LebExportService(
    INoeLebContractBuilder builder, LebExportValidator validator,
    ICsvLebExporter csv, IExcelLebExporter excel) : ILebExportService
{
    public async Task<ValidationResult> ValidateAsync(
        LebExportRequest request, CancellationToken ct) =>
        validator.Validate(await builder.BuildAsync(request, ct));

    public async Task<LebExportFile> ExportCsvAsync(
        LebExportRequest request, CancellationToken ct)
    {
        var contract = await ValidatedContract(request, ct);
        return csv.Export(contract);
    }

    public async Task<LebExportFile> ExportExcelAsync(
        LebExportRequest request, CancellationToken ct)
    {
        var contract = await ValidatedContract(request, ct);
        return excel.Export(contract);
    }

    private async Task<Contracts.NoeLebExportContractV1> ValidatedContract(
        LebExportRequest request, CancellationToken ct)
    {
        var dataset = await builder.BuildAsync(request, ct);
        var validation = validator.Validate(dataset);
        if (!validation.CanExport) throw new LebExportValidationException(validation);
        return dataset.Contract;
    }
}
