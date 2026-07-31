using Enset.Application.Imports.DuplicationCheck.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Mapping;
using Enset.Application.Imports.DuplicationCheck.Validation;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DuplicationCheck.Services;

public class DuplicationCheckService : IDuplicationCheckService
{
    public IReadOnlyCollection<ImportIssue> DetectCustomers(
        IReadOnlyCollection<CustomerImportDto> customers)
    {
        var validator = new CustomerDuplicateValidator();

        var candidates = validator.FindDuplicates(customers);

        return candidates
            .Select(CustomerDuplicateIssueMapper.ToIssue)
            .ToList();
    }
}