using Enset.Domain.Customers;

namespace Enset.Domain.Users;

/// <summary>
/// Assigns a customer-scoped role to a user for a bounded validity period.
/// </summary>
public sealed class UserCustomerAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid CustomerId { get; set; }

    public UserCustomerRole Role { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public bool IsValidAt(DateTimeOffset timestamp) =>
        IsActive && ValidFrom <= timestamp &&
        (ValidTo is null || ValidTo > timestamp);
}
