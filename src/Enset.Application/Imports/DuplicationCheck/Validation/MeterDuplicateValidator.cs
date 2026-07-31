using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Models;

namespace Enset.Application.Imports.DuplicationCheck.Validation;

public class MeterDuplicateValidator
{
    public List<DuplicateCandidate<MeterImportDto>> FindDuplicates(
        IEnumerable<MeterImportDto> meters)
    {
        throw new NotImplementedException();
    }
}