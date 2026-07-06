using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;

namespace Enset.Application.Imports.WriteGate;

public class ImportWriteGate : IImportWriteGate
{
    public bool CanWrite(ImportWriteContext context)
    {
    if (context.Decision == ImportDecisionType.Abort)
        return false;

    if (!context.UserConfirmed)
        return false;

    if (context.Issues.Any(issue =>
        issue.RequiresUserDecision &&
        !issue.IsResolved))
    {
        return false;
    }

    return true;
    }
}
