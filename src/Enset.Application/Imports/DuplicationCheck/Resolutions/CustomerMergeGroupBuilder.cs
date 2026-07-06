using Enset.Application.Imports.DuplicationCheck.Models;

namespace Enset.Application.Imports.DuplicationCheck.Resolutions;

public class CustomerMergeGroupBuilder
{
    public List<CustomerMergeGroup> Build(
        IEnumerable<CustomerMergeInstruction> instructions)
    {
        var groups = new List<CustomerMergeGroup>();

        foreach (var instruction in instructions)
        {
            var existingGroup = groups.FirstOrDefault(group =>
                ReferenceEquals(group.MasterCustomer, instruction.MasterCustomer) ||
                group.DuplicateCustomers.Any(duplicate =>
                    ReferenceEquals(duplicate, instruction.MasterCustomer)));

            if (existingGroup is null)
            {
                groups.Add(new CustomerMergeGroup
                {
                    MasterCustomer = instruction.MasterCustomer,
                    DuplicateCustomers = [instruction.DuplicateCustomer],
                    ResolvedName = instruction.ResolvedName
                });

                continue;
            }

            if (!existingGroup.DuplicateCustomers.Any(duplicate =>
                    ReferenceEquals(duplicate, instruction.DuplicateCustomer)) &&
                !ReferenceEquals(existingGroup.MasterCustomer, instruction.DuplicateCustomer))
            {
                existingGroup.DuplicateCustomers.Add(instruction.DuplicateCustomer);
            }

            if (!string.IsNullOrWhiteSpace(instruction.ResolvedName))
            {
                existingGroup.ResolvedName = instruction.ResolvedName;
            }
        }

        return groups;
    }
}