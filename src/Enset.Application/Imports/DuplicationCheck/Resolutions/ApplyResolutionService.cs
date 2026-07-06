using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class ApplyResolutionService
{
    public string? GetResolvedValue(ImportResolutionResult result)
    {
        if (result.KeepSeparate)
        {
            return null;
        }

        if (!result.MergeRequired)
        {
            return null;
        }

        return result.ResolvedValue;
    }

    public bool ShouldMerge(ImportResolutionResult result)
    {
        return result.MergeRequired && !result.KeepSeparate;
    }
}