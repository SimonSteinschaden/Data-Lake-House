namespace Enset.Application.Authorization;

/// <summary>
/// Resolves an active application user from an external identity.
/// </summary>
public interface ICurrentUserResolver
{
    Task<ResolvedCurrentUser?> ResolveAsync(
        string externalIdentity,
        CancellationToken cancellationToken = default);
}
