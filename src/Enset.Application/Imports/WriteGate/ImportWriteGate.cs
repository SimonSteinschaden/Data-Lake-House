using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.WriteGate;

public sealed class ImportWriteGate : IImportWriteGate
{
    public ImportWriteGateResult Evaluate(ImportWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<string>();
        var report = context.Report;

        if (report is null)
        {
            errors.Add("Import report is required.");
            return new ImportWriteGateResult { Errors = errors };
        }

        if (context.ImportId == Guid.Empty || context.ImportId != report.ImportId)
            errors.Add("Import id does not match the report.");

        if (string.IsNullOrWhiteSpace(context.UserId))
            errors.Add("User context is required.");

        if (report.Status != ImportStatus.ReadyToCommit)
            errors.Add($"Import status '{report.Status}' is not commit-ready.");

        if (report.Decision.Type == ImportDecisionType.Abort)
            errors.Add("Import decision is Abort.");

        if (context.TargetWriter == ImportWriterType.Excel &&
            string.IsNullOrWhiteSpace(context.TargetLocation))
        {
            errors.Add("An Excel target location is required.");
        }

        if (report.Issues.Any(issue =>
                issue.RequiresUserDecision && !issue.IsResolved))
        {
            errors.Add("At least one issue requiring a user decision is unresolved.");
        }

        return new ImportWriteGateResult { Errors = errors };
    }

    public bool CanWrite(ImportWriteContext context)
    {
        return Evaluate(context).IsAllowed;
    }
}
