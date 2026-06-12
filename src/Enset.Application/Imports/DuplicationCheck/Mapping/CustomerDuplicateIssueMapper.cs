using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Models;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DuplicationCheck.Mapping;

public static class CustomerDuplicateIssueMapper
{
    public static ImportIssue ToIssue(
        DuplicateCandidate<CustomerImportDto> candidate)
    {
        return DuplicateCandidateIssueMapper.ToImportIssue(
            candidate,
            ImportIssueType.DuplicateCustomer,
            candidate.First.CompanyName,
            candidate.Second.CompanyName,
            nameof(CustomerImportDto.CompanyName));
    }
}