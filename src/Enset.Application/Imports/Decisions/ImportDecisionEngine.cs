using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Decisions;

public static class ImportDecisionEngine
{
    public static ImportDecision Decide(ImportReport report)
    {
        if (report.HasOpenCommitBlockingIssues)
        {
            return new ImportDecision
            {
                Type = ImportDecisionType.Abort,
                Reason = "The import contains unresolved commit-blocking issues."
            };
        }

        return new ImportDecision
        {
            Type = ImportDecisionType.Continue,
            Reason = "The import contains no unresolved commit-blocking issues."
        };
    }
}
