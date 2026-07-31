using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Reports;
using Enset.Infrastructure.Imports.Persistence.Mappings;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Imports.Persistence.Repositories;

public sealed class EfImportReportRepository : IImportReportRepository
{
    private readonly EnsetDbContext _dbContext;

    public EfImportReportRepository(EnsetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        ImportReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var exists = await _dbContext.ImportReports
            .AnyAsync(
                entity => entity.ImportId == report.ImportId,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Import report '{report.ImportId}' already exists.");
        }

        var entity = ImportReportPersistenceMapper.ToEntity(report);

        _dbContext.ImportReports.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ImportReport?> GetAsync(
        Guid importId,
        CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(importId, cancellationToken);
    }

    public async Task<ImportReport?> GetByIdAsync(
        Guid importId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ImportReports
            .AsNoTracking()
            .Include(report => report.Issues)
            .Include(report => report.AuditTrail)
            .FirstOrDefaultAsync(
                report => report.ImportId == importId,
                cancellationToken);

        return entity is null
            ? null
            : ImportReportPersistenceMapper.ToModel(entity);
    }

    public async Task UpdateAsync(
        ImportReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var existing = await _dbContext.ImportReports
            .Include(entity => entity.Issues)
            .Include(entity => entity.AuditTrail)
            .FirstOrDefaultAsync(
                entity => entity.ImportId == report.ImportId,
                cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException(
                $"Import report '{report.ImportId}' does not exist.");
        }

        var updated = ImportReportPersistenceMapper.ToEntity(report);
        updated.UpdatedAt = DateTime.UtcNow;

        existing.Status = updated.Status;
        existing.SourceFileName = updated.SourceFileName;
        existing.SourceFileContentType = updated.SourceFileContentType;
        existing.SourceFileSizeBytes = updated.SourceFileSizeBytes;
        existing.SourceFileSha256 = updated.SourceFileSha256;
        existing.SourceFileStagedPath = updated.SourceFileStagedPath;
        existing.SourceFileRawStoragePath = updated.SourceFileRawStoragePath;
        existing.CustomerCount = updated.CustomerCount;
        existing.BuildingCount = updated.BuildingCount;
        existing.CreatedAt = updated.CreatedAt;
        existing.UpdatedAt = updated.UpdatedAt;

        _dbContext.ImportIssues.RemoveRange(existing.Issues);
        _dbContext.ImportAuditEntries.RemoveRange(existing.AuditTrail);

        existing.Issues.Clear();
        existing.AuditTrail.Clear();

        foreach (var issue in updated.Issues)
            existing.Issues.Add(issue);

        foreach (var audit in updated.AuditTrail)
            existing.AuditTrail.Add(audit);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
