using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Decisions;

public static class ImportDecisionEngine
{
    public static ImportDecisionType Decide(ImportReport report)
    {
        if (report.HasErrors)
            return ImportDecisionType.Abort;

        return ImportDecisionType.Continue;
    }
}