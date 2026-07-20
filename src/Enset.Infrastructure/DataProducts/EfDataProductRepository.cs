using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Domain.DataProducts;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.DataProducts;

public sealed class EfDataProductRepository : IDataProductRepository,
    IDataProductGenerationRunRepository
{
    private readonly EnsetDbContext _db;
    public EfDataProductRepository(EnsetDbContext db) => _db = db;

    public Task<DataProduct?> GetForGenerationAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DataProducts.Include(x => x.Definition).Include(x => x.ScopeAssignments)
            .Include(x => x.CustomerAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DataProduct>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.DataProducts.AsNoTracking().Include(x => x.Definition)
            .Include(x => x.ScopeAssignments).Include(x => x.Versions)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<DataProductVersion?> GetLatestVersionAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DataProductVersions.AsNoTracking().Include(x => x.Values)
            .Include(x => x.GenerationRun).Where(x => x.DataProductId == id)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DataProductVersion>> GetVersionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.DataProductVersions.AsNoTracking().Include(x => x.Values)
            .Include(x => x.GenerationRun).Where(x => x.DataProductId == id)
            .OrderByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);

    public async Task<int> GetNextVersionNumberAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await _db.DataProductVersions.Where(x => x.DataProductId == id)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0) + 1;

    public async Task AddVersionAsync(DataProductVersion version, CancellationToken cancellationToken = default)
    { _db.DataProductVersions.Add(version); await _db.SaveChangesAsync(cancellationToken); }

    public async Task AddAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default)
    { _db.DataProductGenerationRuns.Add(run); await _db.SaveChangesAsync(cancellationToken); }

    public Task UpdateAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
