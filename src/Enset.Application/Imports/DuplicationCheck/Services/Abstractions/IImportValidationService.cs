using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Services.Abstractions;

public interface IImportValidationService
{
    List<ImportIssue> ValidateCustomers(
        IEnumerable<CustomerImportDto> customers);
}