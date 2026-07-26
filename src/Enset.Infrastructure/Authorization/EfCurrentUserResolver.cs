using Enset.Application.Authorization;
using Enset.Domain.Users;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Authorization;

public sealed class EfCurrentUserResolver : ICurrentUserResolver
{
    private readonly EnsetDbContext _db;

    public EfCurrentUserResolver(EnsetDbContext db) => _db = db;

    public async Task<ResolvedCurrentUser?> ResolveAsync(
        string externalIdentity,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = await _db.ApplicationUsers
            .AsNoTracking()
            .Where(candidate => candidate.IsActive &&
                candidate.ExternalIdentity == externalIdentity)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.ExternalIdentity,
                candidate.GlobalRole,
                CustomerRoles = candidate.CustomerAssignments
                    .Where(assignment => assignment.IsActive &&
                        assignment.ValidFrom <= now &&
                        (assignment.ValidTo == null || assignment.ValidTo > now))
                    .Select(assignment => assignment.Role)
                    .Distinct()
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var roles = user.CustomerRoles.Select(role => role.ToString()).ToList();
        if (user.GlobalRole is { } globalRole)
            roles.Add(globalRole.ToString());

        return new ResolvedCurrentUser(
            user.Id,
            user.ExternalIdentity,
            user.GlobalRole is GlobalUserRole.EnsetEmployee or GlobalUserRole.EnsetAdmin,
            roles);
    }
}
