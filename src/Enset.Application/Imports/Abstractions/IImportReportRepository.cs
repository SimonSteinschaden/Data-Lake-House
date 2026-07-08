using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Abstractions;

public interface IImportReportRepository
{
    Task SaveAsync(ImportReport report, CancellationToken cancellationToken = default);
    Task<ImportReport?> GetByIdAsync(Guid importId, CancellationToken cancellationToken = default);
    Task UpdateAsync(ImportReport report, CancellationToken cancellationToken = default);
}
