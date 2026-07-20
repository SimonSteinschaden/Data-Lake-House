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
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

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
