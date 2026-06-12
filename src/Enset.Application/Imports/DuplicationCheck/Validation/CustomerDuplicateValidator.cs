using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Identity;
using Enset.Application.Imports.DuplicationCheck.Models;

namespace Enset.Application.Imports.DuplicationCheck.Validation;

public class CustomerDuplicateValidator
{
    public List<DuplicateCandidate<CustomerImportDto>> FindDuplicates(
        IEnumerable<CustomerImportDto> customers)
    {
        var list = customers.ToList();
        var result = new List<DuplicateCandidate<CustomerImportDto>>();

        var exactDuplicates = list
            .GroupBy(x => CustomerIdentityKeyBuilder.Build(x).ExactKey)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Where(g => g.Count() > 1);

        foreach (var group in exactDuplicates)
        {
            var items = group.ToList();

            for (var i = 0; i < items.Count - 1; i++)
            {
                result.Add(new DuplicateCandidate<CustomerImportDto>
                {
                    First = items[i],
                    Second = items[i + 1],
                    SimilarityScore = 1.0,
                    Reason = "Exact customer identity key match",
                    RequiresUserDecision = true,
                    ProposedResolution = items[i].CompanyName
                });
            }
        }

        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var nameA = CustomerIdentityKeyBuilder.Normalize(list[i].CompanyName);
                var nameB = CustomerIdentityKeyBuilder.Normalize(list[j].CompanyName);

                if (string.IsNullOrWhiteSpace(nameA) ||
                    string.IsNullOrWhiteSpace(nameB))
                {
                    continue;
                }

                var similarity = StringSimilarity.Similarity(nameA, nameB);

                if (similarity >= 0.88)
                {
                    result.Add(new DuplicateCandidate<CustomerImportDto>
                    {
                        First = list[i],
                        Second = list[j],
                        SimilarityScore = similarity,
                        Reason = "Similar customer name",
                        RequiresUserDecision = true,
                        ProposedResolution = list[i].CompanyName
                    });
                }
            }
        }

        return result;
    }
}