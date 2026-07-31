using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.WriteGate;


namespace Enset.Application.Imports.Coordination;

public sealed class ImportCommitService : IImportCommitService
{
    private readonly IImportReportRepository _reports;
    private readonly IImportWriteGate _writeGate;
    private readonly IReadOnlyCollection<IImportWriter> _writers;
    private readonly IRawZoneWriter? _rawZoneWriter;

    public ImportCommitService(
        IImportReportRepository reports,
        IImportWriteGate writeGate,
        IEnumerable<IImportWriter> writers,
        IRawZoneWriter? rawZoneWriter = null)
    {
        _reports = reports;
        _writeGate = writeGate;
        _writers = writers.ToList();
        _rawZoneWriter = rawZoneWriter;
    }

    public async Task<ImportCommitResult> CommitAsync(
        ImportCommitCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var report = await _reports.GetAsync(command.ImportId, cancellationToken);
        var context = new ImportWriteContext
        {
            ImportId = command.ImportId,
            Report = report,
            TargetMode = command.TargetMode,
            TargetWriter = command.TargetWriter,
            UserId = command.UserId,
            Timestamp = command.Timestamp,
            TargetLocation = command.TargetLocation,
            ArchiveRawSource = command.ArchiveRawSource
        };

        var gateResult = _writeGate.Evaluate(context);
        if (!gateResult.IsAllowed)
        {
            return new ImportCommitResult
            {
                Succeeded = false,
                Report = report,
                GateResult = gateResult
            };
        }

        var writer = _writers.SingleOrDefault(candidate =>
            candidate.WriterType == command.TargetWriter);

        if (writer is null)
        {
            return new ImportCommitResult
            {
                Succeeded = false,
                Report = report,
                GateResult = new ImportWriteGateResult
                {
                    Errors = [$"No writer is registered for target '{command.TargetWriter}'."]
                }
            };
        }

        report!.Status = ImportStatus.Committing;
        report.UpdatedAt = command.Timestamp;
        report.AuditTrail.Add(CreateAudit(
            command,
            "CommitStarted",
            $"Writer={command.TargetWriter}; Mode={command.TargetMode}"));
        await _reports.SaveAsync(report, cancellationToken);

        try
        {
            await writer.WriteAsync(context, cancellationToken);

            if (command.ArchiveRawSource && _rawZoneWriter is not null)
            {
                report.SourceFile!.RawStoragePath =
                    await _rawZoneWriter.ArchiveAsync(context, cancellationToken);
            }

            report.Status = ImportStatus.Committed;
            report.UpdatedAt = DateTime.UtcNow;
            report.AuditTrail.Add(CreateAudit(command, "CommitCompleted"));
            await _reports.SaveAsync(report, cancellationToken);

            return new ImportCommitResult
            {
                Succeeded = true,
                Report = report,
                GateResult = gateResult
            };
        }
        catch (Exception exception)
        {
            report.Status = ImportStatus.Failed;
            report.UpdatedAt = DateTime.UtcNow;
            report.AuditTrail.Add(CreateAudit(
                command,
                "CommitFailed",
                exception.Message));
            await _reports.SaveAsync(report, cancellationToken);
            throw;
        }
    }

    private static ImportAuditEntry CreateAudit(
        ImportCommitCommand command,
        string action,
        string? details = null)
    {
        return new ImportAuditEntry
        {
            Timestamp = command.Timestamp,
            UserId = command.UserId,
            Action = action,
            Details = details
        };
    }
}
