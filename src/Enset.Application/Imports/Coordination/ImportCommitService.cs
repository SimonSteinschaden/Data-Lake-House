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
        ImportCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = await _reports.GetAsync(request.ImportId, cancellationToken);
        var context = new ImportWriteContext
        {
            ImportId = request.ImportId,
            Report = report,
            TargetMode = request.TargetMode,
            TargetWriter = request.TargetWriter,
            UserId = request.UserId,
            Timestamp = request.Timestamp,
            TargetLocation = request.TargetLocation,
            ArchiveRawSource = request.ArchiveRawSource
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
            candidate.WriterType == request.TargetWriter);

        if (writer is null)
        {
            return new ImportCommitResult
            {
                Succeeded = false,
                Report = report,
                GateResult = new ImportWriteGateResult
                {
                    Errors = [$"No writer is registered for target '{request.TargetWriter}'."]
                }
            };
        }

        report!.Status = ImportStatus.Committing;
        report.UpdatedAt = request.Timestamp;
        report.AuditTrail.Add(CreateAudit(
            request,
            "CommitStarted",
            $"Writer={request.TargetWriter}; Mode={request.TargetMode}"));
        await _reports.SaveAsync(report, cancellationToken);

        try
        {
            await writer.WriteAsync(context, cancellationToken);

            if (request.ArchiveRawSource && _rawZoneWriter is not null)
            {
                report.SourceFile!.RawStoragePath =
                    await _rawZoneWriter.ArchiveAsync(context, cancellationToken);
            }

            report.Status = ImportStatus.Committed;
            report.UpdatedAt = DateTime.UtcNow;
            report.AuditTrail.Add(CreateAudit(request, "CommitCompleted"));
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
                request,
                "CommitFailed",
                exception.Message));
            await _reports.SaveAsync(report, cancellationToken);
            throw;
        }
    }

    private static ImportAuditEntry CreateAudit(
        ImportCommitRequest request,
        string action,
        string? details = null)
    {
        return new ImportAuditEntry
        {
            Timestamp = request.Timestamp,
            UserId = request.UserId,
            Action = action,
            Details = details
        };
    }
}
