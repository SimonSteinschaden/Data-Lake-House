using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.DuplicationCheck.Models;

public class CustomerMergeInstruction
{
    public CustomerImportDto MasterCustomer { get; set; } = default!;

    public CustomerImportDto DuplicateCustomer { get; set; } = default!;

    public string? ResolvedName { get; set; }
}