using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class ImportResolutionResultBuilder
{
    public ImportResolutionResult Build(ImportIssue issue)
    {
        return issue.ResolutionAction switch
        {
            ImportResolutionAction.KeepFirst =>
                new ImportResolutionResult
                {
                    Issue = issue,
                    Action = issue.ResolutionAction,
                    ResolvedValue = issue.FirstValue,
                    MergeRequired = true,
                    KeepSeparate = false
                },

            ImportResolutionAction.KeepSecond =>
                new ImportResolutionResult
                {
                    Issue = issue,
                    Action = issue.ResolutionAction,
                    ResolvedValue = issue.SecondValue,
                    MergeRequired = true,
                    KeepSeparate = false
                },

            ImportResolutionAction.UseCustomValue =>
                new ImportResolutionResult
                {
                    Issue = issue,
                    Action = issue.ResolutionAction,
                    ResolvedValue = issue.CustomResolvedValue,
                    MergeRequired = true,
                    KeepSeparate = false
                },

            ImportResolutionAction.KeepSeparate =>
                new ImportResolutionResult
                {
                    Issue = issue,
                    Action = issue.ResolutionAction,
                    ResolvedValue = null,
                    MergeRequired = false,
                    KeepSeparate = true
                },

            _ => throw new InvalidOperationException(
                $"Unsupported resolution action: {issue.ResolutionAction}")
        };
    }
}