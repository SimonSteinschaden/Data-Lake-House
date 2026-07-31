namespace Enset.Domain.Users;

/// <summary>
/// Defines a user's permissions within one customer tenant.
/// </summary>
public enum UserCustomerRole
{
    CustomerAdmin,
    CustomerUser,
    CustomerViewer
}
