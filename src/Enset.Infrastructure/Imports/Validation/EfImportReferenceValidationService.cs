using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Validation;

public sealed class EfImportReferenceValidationService
    : IImportReferenceValidationService
{
    private readonly EnsetDbContext _dbContext;

    public EfImportReferenceValidationService(EnsetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ImportIssue>> ValidateAsync(
        ImportWorkbook workbook,
        CancellationToken cancellationToken = default)
    {
        if (workbook.SourceType != ImportSourceType.Csv)
            return [];

        var referencedMeters = workbook.MeterReadings
            .Where(reading => !string.IsNullOrWhiteSpace(reading.MeterNumber))
            .GroupBy(
                reading => reading.MeterNumber!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var meterNumbers = referencedMeters
            .Select(group => group.Key)
            .ToList();

        var existingMeterNumbers = await _dbContext.Meters
            .AsNoTracking()
            .Where(meter => meterNumbers.Contains(meter.MeterNumber))
            .Select(meter => meter.MeterNumber)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        return referencedMeters
            .Where(group => !existingMeterNumbers.Contains(group.Key))
            .Select(group => new ImportIssue
            {
                Type = ImportIssueType.MissingMeter,
                Severity = ImportIssueSeverity.Error,
                Message =
                    $"CSV rows {string.Join(", ", group.Select(row => row.RowNumber))}: " +
                    $"MeterNumber '{group.Key}' does not exist in PostgreSQL.",
                FieldName = "MeterNumber",
                FirstValue = group.Key,
                RequiresUserDecision = false
            })
            .ToList();
    }
}
