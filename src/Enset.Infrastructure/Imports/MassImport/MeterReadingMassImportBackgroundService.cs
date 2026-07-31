using System.Collections.Concurrent;
using System.Threading.Channels;
using Enset.Application.Imports.MassImport;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enset.Infrastructure.Imports.MassImport;

public sealed class MeterReadingMassImportBackgroundService(
    IServiceScopeFactory scopes,
    ILogger<MeterReadingMassImportBackgroundService> logger)
    : BackgroundService, IMeterReadingMassImportJobs
{
    private readonly Channel<Guid> queue =
        Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource>
        cancellations = new();

    public async Task<MeterReadingImportAccepted> Enqueue(
        StartMeterReadingImportJob command,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<EnsetDbContext>();
        var existing = await db.MeterReadingImportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ImportId == command.ImportId,
                cancellationToken);
        if (existing is not null &&
            !Terminal(existing.Status))
            return Accepted(existing);
        if (existing is not null)
            db.MeterReadingImportJobs.Remove(existing);
        var now = DateTime.UtcNow;
        var job = new MeterReadingImportJobEntity
        {
            JobId = Guid.NewGuid(),
            ImportId = command.ImportId,
            TargetMode = command.TargetMode.ToString(),
            Status = MeterReadingImportJobStatus.Queued.ToString(),
            Phase = MeterReadingImportJobStatus.Queued.ToString(),
            UserId = command.UserId,
            TotalBytes = command.TotalBytes,
            AcceptedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.MeterReadingImportJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await queue.Writer.WriteAsync(job.JobId, cancellationToken);
        return Accepted(job);
    }

    public async Task<MeterReadingImportJobProgress?> Get(
        Guid importId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<EnsetDbContext>();
        var job = await db.MeterReadingImportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ImportId == importId,
                cancellationToken);
        return job is null ? null : Map(job);
    }

    public async Task<MeterReadingImportCancelled?> Cancel(
        Guid importId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<EnsetDbContext>();
        var job = await db.MeterReadingImportJobs.SingleOrDefaultAsync(
            x => x.ImportId == importId,
            cancellationToken);
        if (job is null)
            return null;
        var previous = Parse(job.Status);
        if (!Terminal(job.Status))
        {
            job.CancellationRequested = true;
            job.Status = MeterReadingImportJobStatus.Cancelled.ToString();
            job.Phase = MeterReadingImportJobStatus.Cancelled.ToString();
            job.CompletedAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            if (cancellations.TryGetValue(job.JobId, out var source))
                await source.CancelAsync();
        }
        return new(
            job.ImportId,
            job.JobId,
            previous,
            Parse(job.Status));
    }

    public override async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<EnsetDbContext>();
        await db.MeterReadingImportJobs
            .Where(x =>
                x.Status != "Completed" &&
                x.Status != "Failed" &&
                x.Status != "Cancelled" &&
                x.Status != "Interrupted")
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.Status, "Interrupted")
                    .SetProperty(x => x.Phase, "Interrupted")
                    .SetProperty(x => x.ErrorCode, "API_RESTART")
                    .SetProperty(
                        x => x.ErrorMessage,
                        "The API stopped before the job completed.")
                    .SetProperty(x => x.CompletedAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            using var linked = CancellationTokenSource
                .CreateLinkedTokenSource(stoppingToken);
            cancellations[jobId] = linked;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IMeterReadingMassImportProcessor>();
                await processor.Process(jobId, linked.Token);
            }
            catch (OperationCanceledException)
            {
                await Fail(
                    jobId,
                    MeterReadingImportJobStatus.Cancelled,
                    "CANCELLED",
                    "The import was cancelled.",
                    stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Meter reading import failed. JobId={JobId}",
                    jobId);
                await Fail(
                    jobId,
                    MeterReadingImportJobStatus.Failed,
                    "IMPORT_FAILED",
                    exception.Message,
                    stoppingToken);
            }
            finally
            {
                cancellations.TryRemove(jobId, out _);
            }
        }
    }

    private async Task Fail(
        Guid jobId,
        MeterReadingImportJobStatus status,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider
                .GetRequiredService<EnsetDbContext>();
            var job = await db.MeterReadingImportJobs.SingleOrDefaultAsync(
                x => x.JobId == jobId,
                cancellationToken);
            if (job is null)
                return;
            job.Status = status.ToString();
            job.Phase = status.ToString();
            job.ErrorCode = code;
            job.ErrorMessage = message;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not persist terminal import status. JobId={JobId}",
                jobId);
        }
    }

    private static MeterReadingImportAccepted Accepted(
        MeterReadingImportJobEntity job) =>
        new(
            job.ImportId,
            job.JobId,
            Parse(job.Status),
            job.AcceptedAtUtc,
            $"/api/v1/imports/{job.ImportId:D}/status");

    private static MeterReadingImportJobProgress Map(
        MeterReadingImportJobEntity job) =>
        new(
            job.ImportId,
            job.JobId,
            Parse(job.Status),
            job.Phase,
            job.TotalBytes,
            job.ProcessedBytes,
            job.ReadRows,
            job.StagedRows,
            job.ValidRows,
            job.RejectedRows,
            job.DuplicateRows,
            job.WrittenRows,
            job.CurrentBatch,
            job.StartedAtUtc,
            job.UpdatedAtUtc,
            job.CompletedAtUtc,
            job.ErrorCode,
            job.ErrorMessage);

    private static MeterReadingImportJobStatus Parse(string value) =>
        Enum.TryParse<MeterReadingImportJobStatus>(
            value,
            out var parsed)
            ? parsed
            : MeterReadingImportJobStatus.Failed;

    private static bool Terminal(string value) =>
        value is "Completed" or "Failed" or "Cancelled" or "Interrupted";
}
