using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Abstractions;

public interface IImportReferenceValidationService
{
    Task<IReadOnlyCollection<ImportIssue>> ValidateAsync(
        ImportWorkbook workbook,
        CancellationToken cancellationToken = default);
}
