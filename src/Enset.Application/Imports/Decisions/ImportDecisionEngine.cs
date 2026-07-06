using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Decisions;

public static class ImportDecisionEngine
{
    public static ImportDecision Decide(ImportReport report)
    {
        if (report.HasErrors)
        {
            return new ImportDecision
            {
                Type = ImportDecisionType.Abort,
                Reason = "The import contains one or more errors."
            };
        }

        return new ImportDecision
        {
            Type = ImportDecisionType.Continue,
            Reason = "The import contains no errors."
        };
    }
}
