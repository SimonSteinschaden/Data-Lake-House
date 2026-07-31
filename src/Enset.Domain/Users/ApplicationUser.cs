namespace Enset.Domain.Users;

/// <summary>
/// Represents an ENSET user linked to an external identity provider identity.
/// </summary>
public sealed class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ExternalIdentity { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public GlobalUserRole? GlobalRole { get; set; }

    public ICollection<UserCustomerAssignment> CustomerAssignments { get; set; }
        = new List<UserCustomerAssignment>();
}
