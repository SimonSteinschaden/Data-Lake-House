namespace Enset.Application.Authorization;

/// <summary>
/// Contains identity and role data resolved from the persistence boundary.
/// </summary>
public sealed record ResolvedCurrentUser(
    Guid UserId,
    string ExternalIdentity,
    bool IsEnsetEmployee,
    IReadOnlyCollection<string> Roles);
