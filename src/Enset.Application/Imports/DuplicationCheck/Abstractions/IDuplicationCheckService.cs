using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DuplicationCheck.Abstractions;

public interface IDuplicationCheckService
{
    IReadOnlyCollection<ImportIssue> DetectCustomers(
        IReadOnlyCollection<CustomerImportDto> customers);

    // TODO:
    // IReadOnlyCollection<ImportIssue> DetectBuildings(...);

    // IReadOnlyCollection<ImportIssue> DetectMeters(...);
}