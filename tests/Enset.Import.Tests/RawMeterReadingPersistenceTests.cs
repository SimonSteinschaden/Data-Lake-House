using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.WriteGate;
using Enset.Domain.Energy;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class RawMeterReadingPersistenceTests
{
    [Fact]
    public async Task MissingValueAndTimestampAreStoredRawWithoutCuratedDefaults()
    {
        await using var db = CreateDatabase();
        var importId = Guid.NewGuid();
        var report = Report(importId,
            Reading(2, "KNOWN", null, null, "", ""),
            Reading(3, "KNOWN", null, 12.5m, "", "12,5"));

        await WriteAsync(db, report);

        var raw = await db.ImportedMeterReadings
            .OrderBy(reading => reading.RowNumber)
            .ToListAsync();
        Assert.Equal(2, raw.Count);
        Assert.All(raw, reading => Assert.Null(reading.Timestamp));
        Assert.Null(raw[0].Value);
        Assert.Equal(string.Empty, raw[0].TimestampRaw);
        Assert.Equal(string.Empty, raw[0].ValueRaw);
        Assert.DoesNotContain(
            raw,
            reading => reading.Timestamp == DateTime.MinValue);
        Assert.Empty(await db.MeterReadings.ToListAsync());
    }

    [Fact]
    public async Task UnknownMeterRemainsRawAndDoesNotCreateCuratedReading()
    {
        await using var db = CreateDatabase();
        var report = Report(
            Guid.NewGuid(),
            Reading(
                2,
                "UNKNOWN-4711",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                7.5m,
                "2026-01-01T00:00:00Z",
                "7,5"));

        await WriteAsync(db, report);

        var raw = Assert.Single(await db.ImportedMeterReadings.ToListAsync());
        Assert.Equal("UNKNOWN-4711", raw.MeterNumberRaw);
        Assert.Null(raw.MeterId);
        Assert.Empty(await db.MeterReadings.ToListAsync());
    }

    [Fact]
    public async Task ValidResolvedRawReadingCreatesLinkedCuratedReading()
    {
        await using var db = CreateDatabase();
        var meter = new Meter
        {
            MeterNumber = "KNOWN",
            Name = "Known meter"
        };
        db.Meters.Add(meter);
        await db.SaveChangesAsync();
        var timestamp =
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var report = Report(
            Guid.NewGuid(),
            Reading(2, "KNOWN", timestamp, 42.25m,
                "2026-01-01T00:00:00Z", "42,25"));

        await WriteAsync(db, report);

        var raw = Assert.Single(await db.ImportedMeterReadings.ToListAsync());
        var curated = Assert.Single(await db.MeterReadings.ToListAsync());
        Assert.Equal(meter.Id, raw.MeterId);
        Assert.Equal(42.25m, curated.Value);
        Assert.Equal(raw.Id, curated.SourceRawReadingId);
        Assert.Equal(report.ImportId, curated.SourceImportJobId);
    }

    [Fact]
    public async Task MultipleDerivedReadingsMayReferenceTheSameSourceRow()
    {
        await using var db = CreateDatabase();
        var report = Report(
            Guid.NewGuid(),
            Reading(7, "UNKNOWN",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                1m, "2026-01-01", "1"),
            Reading(7, "UNKNOWN",
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                2m, "2026-02-01", "2"));

        await WriteAsync(db, report);

        Assert.Equal(2, await db.ImportedMeterReadings.CountAsync());
    }

    private static EnsetDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics
                        .InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new EnsetDbContext(options);
    }

    private static ImportReport Report(
        Guid importId,
        params MeterReadingImportDto[] readings) => new()
    {
        ImportId = importId,
        SourceType = ImportSourceType.Csv,
        MeterReadings = readings
    };

    private static MeterReadingImportDto Reading(
        int row,
        string meterNumber,
        DateTime? timestamp,
        decimal? value,
        string timestampRaw,
        string valueRaw) => new()
    {
        RowNumber = row,
        MeterNumber = meterNumber,
        MeterNumberRaw = meterNumber,
        Timestamp = timestamp,
        TimestampRaw = timestampRaw,
        Value = value,
        ValueRaw = valueRaw
    };

    private static Task WriteAsync(
        EnsetDbContext db,
        ImportReport report) =>
        new DatabaseImportWriter(db).WriteAsync(new ImportWriteContext
        {
            ImportId = report.ImportId,
            Report = report,
            TargetMode = ImportTargetMode.Upsert,
            TargetWriter = ImportWriterType.Database,
            UserId = "raw-reading-test"
        });
}
