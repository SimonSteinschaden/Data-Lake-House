using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Models;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class CustomerMergeInstructionBuilder
{
    public CustomerMergeInstruction? Build(
        DuplicateCandidate<CustomerImportDto> candidate,
        ImportResolutionResult result)
    {
        if (result.KeepSeparate || !result.MergeRequired)
        {
            return null;
        }

        return result.Action switch
        {
            ImportResolutionAction.KeepFirst =>
                new CustomerMergeInstruction
                {
                    MasterCustomer = candidate.First,
                    DuplicateCustomer = candidate.Second,
                    ResolvedName = result.ResolvedValue
                },

            ImportResolutionAction.KeepSecond =>
                new CustomerMergeInstruction
                {
                    MasterCustomer = candidate.Second,
                    DuplicateCustomer = candidate.First,
                    ResolvedName = result.ResolvedValue
                },

            ImportResolutionAction.UseCustomValue =>
                new CustomerMergeInstruction
                {
                    MasterCustomer = candidate.First,
                    DuplicateCustomer = candidate.Second,
                    ResolvedName = result.ResolvedValue
                },

            _ => null
        };
    }
}