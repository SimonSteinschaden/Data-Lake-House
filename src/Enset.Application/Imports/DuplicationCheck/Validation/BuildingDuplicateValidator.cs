using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Models;

namespace Enset.Application.Imports.DuplicationCheck.Validation;

public class BuildingDuplicateValidator
{
    public List<DuplicateCandidate<BuildingImportDto>> FindDuplicates(
        IEnumerable<BuildingImportDto> buildings)
    {
        throw new NotImplementedException();
    }
}