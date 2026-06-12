using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class ApplyResolutionService
{
    public string? GetResolvedValue(ImportIssue issue)
    {
        return issue.ResolutionAction switch
        {
            ImportResolutionAction.KeepFirst => issue.FirstValue,
            ImportResolutionAction.KeepSecond => issue.SecondValue,
            ImportResolutionAction.UseCustomValue => issue.CustomResolvedValue,
            ImportResolutionAction.KeepSeparate => null,
            _ => null
        };
    }
}