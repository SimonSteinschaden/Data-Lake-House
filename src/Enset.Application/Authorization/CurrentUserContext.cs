namespace Enset.Application.Authorization;

/// <summary>
/// Stores the resolved user identity for the lifetime of one application scope.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private IReadOnlyCollection<string> _roles = Array.Empty<string>();

    public Guid? UserId { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public bool IsEnsetEmployee { get; private set; }

    public IReadOnlyCollection<string> Roles => _roles;

    public void Initialize(Guid userId, bool isEnsetEmployee, IEnumerable<string> roles)
    {
        if (UserId.HasValue)
            throw new InvalidOperationException("The current user context is already initialized.");

        UserId = userId;
        IsEnsetEmployee = isEnsetEmployee;
        _roles = roles.Distinct(StringComparer.Ordinal).ToArray();
    }
}
