namespace Enset.Application.Authorization;

/// <summary>
/// Provides the authenticated ENSET user without coupling application code to HTTP.
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsEnsetEmployee { get; }

    IReadOnlyCollection<string> Roles { get; }
}
