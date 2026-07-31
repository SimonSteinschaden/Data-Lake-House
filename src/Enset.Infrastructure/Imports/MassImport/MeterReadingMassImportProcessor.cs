using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.MassImport;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Enset.Infrastructure.Imports.MassImport;

public sealed class MeterReadingMassImportOptions
{
    public int ChunkSize { get; set; } = 10_000;
    public int SuccessfulRetentionDays { get; set; } = 7;
    public int FailedRetentionDays { get; set; } = 30;
    public int BulkCommandTimeoutSeconds { get; set; } = 300;
}

public interface IMeterReadingMassImportProcessor
{
    Task Process(
        Guid jobId,
        CancellationToken cancellationToken);
}

public sealed class MeterReadingMassImportProcessor(
    EnsetDbContext db,
    IImportReportRepository reports,
    IMeterReadingStreamingReader reader,
    IOptions<MeterReadingMassImportOptions> options,
    ILogger<MeterReadingMassImportProcessor> logger)
    : IMeterReadingMassImportProcessor
{
    public async Task Process(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await db.MeterReadingImportJobs
            .SingleAsync(x => x.JobId == jobId, cancellationToken);
        await EnsureNotCancelled(job, cancellationToken);
        await CleanupExpired(cancellationToken);
        var report = await reports.GetAsync(
            job.ImportId,
            cancellationToken) ??
            throw new InvalidOperationException(
                $"Import {job.ImportId} was not found.");
        var sourcePath = report.SourceFile?.StagedPath;
        if (string.IsNullOrWhiteSpace(sourcePath) ||
            !File.Exists(sourcePath))
            throw new FileNotFoundException(
                "The staged import source is unavailable.",
                sourcePath);

        job.StartedAtUtc = DateTime.UtcNow;
        await Status(job, MeterReadingImportJobStatus.Reading, db,
            cancellationToken);
        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous |
            FileOptions.SequentialScan);
        var batch = 0;
        await foreach (var chunk in reader.ReadChunks(
                           stream,
                           report.AssignedMeterId,
                           report.DefaultMeterNumber,
                           options.Value.ChunkSize,
                           cancellationToken))
        {
            await EnsureNotCancelled(job, cancellationToken);
            batch++;
            job.CurrentBatch = batch;
            job.ReadRows += chunk.Count;
            job.ProcessedBytes = stream.Position;
            await Status(job, MeterReadingImportJobStatus.Staging, db,
                cancellationToken);
            await CopyChunk(
                job,
                chunk,
                cancellationToken);
            job.StagedRows += chunk.Count;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            job = await db.MeterReadingImportJobs
                .SingleAsync(x => x.JobId == jobId, cancellationToken);
        }

        await Status(job, MeterReadingImportJobStatus.Validating, db,
            cancellationToken);
        await ValidateAndDeduplicate(job, cancellationToken);
        await Status(job, MeterReadingImportJobStatus.Writing, db,
            cancellationToken);
        await Write(job, cancellationToken);
        await CreateAudit(job, cancellationToken);
        report.Status = ImportStatus.Committed;
        report.UpdatedAt = DateTime.UtcNow;
        await reports.SaveAsync(report, cancellationToken);
        job.CompletedAtUtc = DateTime.UtcNow;
        await Status(job, MeterReadingImportJobStatus.Completed, db,
            cancellationToken);
        logger.LogInformation(
            "Meter reading mass import completed. ImportId={ImportId} " +
            "JobId={JobId} Read={ReadRows} Written={WrittenRows} " +
            "Rejected={RejectedRows} Duplicates={DuplicateRows}",
            job.ImportId,
            job.JobId,
            job.ReadRows,
            job.WrittenRows,
            job.RejectedRows,
            job.DuplicateRows);
    }

    private async Task CopyChunk(
        MeterReadingImportJobEntity job,
        IReadOnlyList<MeterReadingStagingRow> chunk,
        CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);
        await using var transaction = await connection
            .BeginTransactionAsync(ct);
        var importer = await connection.BeginBinaryImportAsync(
            """
            COPY "ImportStagingMeterReadings"
            ("ImportId","JobId","BatchNumber","SourceRowNumber","MeterId",
             "MeterNumberOriginal","Timestamp","Value","Unit","QualityFlag",
             "ReadingType","EnergyDirection","IntervalSeconds",
             "ValidationStatus","ValidationCode","ValidationMessage",
             "RawSourceHash","CreatedAtUtc")
            FROM STDIN (FORMAT BINARY)
            """,
            ct);
        await using (importer)
        {
            foreach (var row in chunk)
            {
                await importer.StartRowAsync(ct);
                await importer.WriteAsync(job.ImportId, NpgsqlDbType.Uuid, ct);
                await importer.WriteAsync(job.JobId, NpgsqlDbType.Uuid, ct);
                await importer.WriteAsync(job.CurrentBatch, NpgsqlDbType.Integer, ct);
                await importer.WriteAsync(row.SourceRowNumber, NpgsqlDbType.Bigint, ct);
                await WriteNullable(importer, row.MeterId, NpgsqlDbType.Uuid, ct);
                await WriteNullable(importer, row.MeterNumber,
                    NpgsqlDbType.Varchar, ct);
                await WriteNullable(importer, row.Timestamp,
                    NpgsqlDbType.TimestampTz, ct);
                await WriteNullable(importer, row.Value,
                    NpgsqlDbType.Numeric, ct);
                await WriteNullable(importer, row.Unit,
                    NpgsqlDbType.Varchar, ct);
                await importer.WriteAsync(row.QualityFlag,
                    NpgsqlDbType.Varchar, ct);
                await importer.WriteAsync(row.ReadingType,
                    NpgsqlDbType.Varchar, ct);
                await WriteNullable(importer, row.EnergyDirection,
                    NpgsqlDbType.Varchar, ct);
                await WriteNullable(importer, row.IntervalSeconds,
                    NpgsqlDbType.Integer, ct);
                await importer.WriteAsync(
                    row.ValidationCode is null ? "Pending" : "Invalid",
                    NpgsqlDbType.Varchar,
                    ct);
                await WriteNullable(importer, row.ValidationCode,
                    NpgsqlDbType.Varchar, ct);
                await WriteNullable(importer, row.ValidationMessage,
                    NpgsqlDbType.Varchar, ct);
                await importer.WriteAsync(row.RawSourceHash,
                    NpgsqlDbType.Varchar, ct);
                await importer.WriteAsync(DateTime.UtcNow,
                    NpgsqlDbType.TimestampTz, ct);
            }
            await importer.CompleteAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    private async Task ValidateAndDeduplicate(
        MeterReadingImportJobEntity job,
        CancellationToken ct)
    {
        db.Database.SetCommandTimeout(
            options.Value.BulkCommandTimeoutSeconds);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "ImportStagingMeterReadings" s
            SET "MeterId" = m."Id"
            FROM "Meters" m
            WHERE s."ImportId" = {job.ImportId}
              AND s."MeterId" IS NULL
              AND m."MeterNumber" = s."MeterNumberOriginal";

            UPDATE "ImportStagingMeterReadings"
            SET "ValidationStatus" = 'Invalid',
                "ValidationCode" = CASE
                    WHEN "MeterId" IS NULL THEN 'METER_NOT_FOUND'
                    WHEN "Timestamp" IS NULL THEN 'TIMESTAMP_INVALID'
                    WHEN "Value" IS NULL THEN 'VALUE_INVALID'
                    WHEN NULLIF(BTRIM("Unit"), '') IS NULL THEN 'UNIT_MISSING'
                    WHEN NULLIF(BTRIM("ReadingType"), '') IS NULL
                        THEN 'READING_TYPE_MISSING'
                    ELSE "ValidationCode"
                END,
                "ValidationMessage" = COALESCE(
                    "ValidationMessage",
                    'Staging validation failed.')
            WHERE "ImportId" = {job.ImportId}
              AND ("MeterId" IS NULL OR "Timestamp" IS NULL OR
                   "Value" IS NULL OR NULLIF(BTRIM("Unit"), '') IS NULL OR
                   NULLIF(BTRIM("ReadingType"), '') IS NULL);

            WITH duplicates AS (
                SELECT "Id", ROW_NUMBER() OVER (
                    PARTITION BY "MeterId", "Timestamp"
                    ORDER BY "SourceRowNumber") AS occurrence
                FROM "ImportStagingMeterReadings"
                WHERE "ImportId" = {job.ImportId}
                  AND "ValidationStatus" <> 'Invalid'
            )
            UPDATE "ImportStagingMeterReadings" s
            SET "ValidationStatus" = 'Duplicate',
                "ValidationCode" = 'DUPLICATE_IN_FILE',
                "ValidationMessage" = 'Duplicate meter/timestamp in file.'
            FROM duplicates d
            WHERE s."Id" = d."Id" AND d.occurrence > 1;

            UPDATE "ImportStagingMeterReadings"
            SET "ValidationStatus" = 'Valid'
            WHERE "ImportId" = {job.ImportId}
              AND "ValidationStatus" = 'Pending';

            UPDATE "ImportStagingMeterReadings" s
            SET "ValidationCode" = CASE
                    WHEN mr."Value" = s."Value" AND
                         mr."ReadingType" = s."ReadingType"
                        THEN 'EXISTING_IDENTICAL'
                    ELSE 'EXISTING_CONFLICT'
                END,
                "ValidationMessage" = CASE
                    WHEN mr."Value" = s."Value" AND
                         mr."ReadingType" = s."ReadingType"
                        THEN 'An identical meter reading already exists.'
                    ELSE 'A conflicting meter reading already exists.'
                END
            FROM "MeterReadings" mr
            WHERE s."ImportId" = {job.ImportId}
              AND s."ValidationStatus" = 'Valid'
              AND mr."MeterId" = s."MeterId"
              AND mr."Timestamp" = s."Timestamp";
            """, ct);
        job.ValidRows = await db.ImportStagingMeterReadings
            .AsNoTracking()
            .LongCountAsync(x =>
                x.ImportId == job.ImportId &&
                x.ValidationStatus == "Valid", ct);
        job.RejectedRows = await db.ImportStagingMeterReadings
            .AsNoTracking()
            .LongCountAsync(x =>
                x.ImportId == job.ImportId &&
                x.ValidationStatus == "Invalid", ct);
        job.DuplicateRows = await db.ImportStagingMeterReadings
            .AsNoTracking()
            .LongCountAsync(x =>
                x.ImportId == job.ImportId &&
                (x.ValidationStatus == "Duplicate" ||
                 x.ValidationCode == "EXISTING_IDENTICAL" ||
                 x.ValidationCode == "EXISTING_CONFLICT"), ct);
        job.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task Write(
        MeterReadingImportJobEntity job,
        CancellationToken ct)
    {
        var update = Enum.TryParse<ImportTargetMode>(
            job.TargetMode,
            out var mode) &&
            mode is ImportTargetMode.Upsert or ImportTargetMode.Replace;
        var conflict = update
            ? """
              DO UPDATE SET
                "Value" = EXCLUDED."Value",
                "ReadingType" = EXCLUDED."ReadingType",
                "QualityFlag" = EXCLUDED."QualityFlag",
                "IntervalSeconds" = EXCLUDED."IntervalSeconds",
                "SourceImportJobId" = EXCLUDED."SourceImportJobId",
                "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
              """
            : "DO NOTHING";
        var sql = $"""
            WITH written AS (
                INSERT INTO "MeterReadings"
                    ("Id","MeterId","Timestamp","Value","ReadingType",
                     "QualityFlag","IntervalSeconds","SourceImportJobId",
                     "CreatedAtUtc","UpdatedAtUtc","IsDeleted",
                     "DataOrigin","LastModifiedSource","LastImportId")
                SELECT gen_random_uuid(), "MeterId", "Timestamp", "Value",
                       "ReadingType", "QualityFlag", "IntervalSeconds",
                       @jobId, NOW(), NOW(), FALSE,
                       'Imported', 'Import', @importId
                FROM "ImportStagingMeterReadings"
                WHERE "ImportId" = @importId
                  AND "ValidationStatus" = 'Valid'
                ON CONFLICT ("MeterId","Timestamp") {conflict}
                RETURNING 1
            )
            SELECT COUNT(*) FROM written;
            """;
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout =
            options.Value.BulkCommandTimeoutSeconds;
        command.Parameters.AddWithValue("jobId", job.JobId);
        command.Parameters.AddWithValue("importId", job.ImportId);
        job.WrittenRows = Convert.ToInt64(
            await command.ExecuteScalarAsync(ct));
        job.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task CleanupExpired(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var successfulBefore =
            now.AddDays(-options.Value.SuccessfulRetentionDays);
        var failedBefore =
            now.AddDays(-options.Value.FailedRetentionDays);
        var expired = await db.MeterReadingImportJobs
            .AsNoTracking()
            .Where(x => x.CompletedAtUtc != null &&
                ((x.Status == "Completed" &&
                  x.CompletedAtUtc < successfulBefore) ||
                 ((x.Status == "Failed" ||
                   x.Status == "Cancelled" ||
                   x.Status == "Interrupted") &&
                  x.CompletedAtUtc < failedBefore)))
            .Select(x => x.ImportId)
            .ToArrayAsync(ct);
        if (expired.Length == 0)
            return;
        await db.ImportStagingMeterReadings
            .Where(x => expired.Contains(x.ImportId))
            .ExecuteDeleteAsync(ct);
    }

    private async Task CreateAudit(
        MeterReadingImportJobEntity job,
        CancellationToken ct)
    {
        var groups = await db.ImportStagingMeterReadings
            .AsNoTracking()
            .Where(x => x.ImportId == job.ImportId)
            .GroupBy(x => x.MeterId)
            .Select(x => new
            {
                MeterId = x.Key,
                Start = x.Min(v => v.Timestamp),
                End = x.Max(v => v.Timestamp),
                Read = x.LongCount(),
                Rejected = x.LongCount(v =>
                    v.ValidationStatus == "Invalid"),
                Duplicates = x.LongCount(v =>
                    v.ValidationStatus == "Duplicate"),
                Written = x.LongCount(v =>
                    v.ValidationStatus == "Valid")
            })
            .ToListAsync(ct);
        db.MeterReadingImportAudits.AddRange(groups.Select(x =>
            new MeterReadingImportAuditEntity
            {
                Id = Guid.NewGuid(),
                ImportId = job.ImportId,
                JobId = job.JobId,
                MeterId = x.MeterId,
                StartTimestamp = x.Start,
                EndTimestamp = x.End,
                ReadCount = x.Read,
                WrittenCount = x.Written,
                RejectedCount = x.Rejected,
                DuplicateCount = x.Duplicates,
                UserId = job.UserId,
                StartedAtUtc = job.StartedAtUtc ?? job.AcceptedAtUtc,
                CompletedAtUtc = DateTime.UtcNow,
                Status = "Completed"
            }));
        await db.SaveChangesAsync(ct);
    }

    private static async Task Status(
        MeterReadingImportJobEntity job,
        MeterReadingImportJobStatus status,
        EnsetDbContext db,
        CancellationToken ct)
    {
        job.Status = status.ToString();
        job.Phase = status.ToString();
        job.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureNotCancelled(
        MeterReadingImportJobEntity job,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (job.CancellationRequested)
            throw new OperationCanceledException(ct);
        await Task.CompletedTask;
    }

    private static async Task WriteNullable<T>(
        NpgsqlBinaryImporter importer,
        T? value,
        NpgsqlDbType type,
        CancellationToken ct)
    {
        if (value is null)
            await importer.WriteNullAsync(ct);
        else
            await importer.WriteAsync(value, type, ct);
    }
}
