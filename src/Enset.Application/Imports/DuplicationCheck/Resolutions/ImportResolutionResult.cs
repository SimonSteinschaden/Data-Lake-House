using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class ImportResolutionResult
{
    public ImportIssue Issue { get; set; } = default!;

    public ImportResolutionAction Action { get; set; }

    public string? ResolvedValue { get; set; }

    public bool MergeRequired { get; set; }

    public bool KeepSeparate { get; set; }
}