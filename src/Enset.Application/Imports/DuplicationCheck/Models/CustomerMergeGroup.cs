using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.DuplicationCheck.Models;

public class CustomerMergeGroup
{
    public CustomerImportDto MasterCustomer { get; set; } = default!;

    public List<CustomerImportDto> DuplicateCustomers { get; set; } = [];

    public string? ResolvedName { get; set; }
}