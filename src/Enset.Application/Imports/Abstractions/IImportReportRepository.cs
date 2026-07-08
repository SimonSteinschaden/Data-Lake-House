using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Abstractions;

public interface IImportReportRepository
{
    Task SaveAsync(ImportReport report, CancellationToken cancellationToken = default);
    Task<ImportReport?> GetAsync(Guid importId, CancellationToken cancellationToken = default);
    //Task UpdateAsync(ImportReport report, CancellationToken cancellationToken = default); // Optional: If you want to support updating existing reports
    //Task DeleteAsync(Guid importId, CancellationToken cancellationToken = default); // Optional: If you want to support deleting reports
}
