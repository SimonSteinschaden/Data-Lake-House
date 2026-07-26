namespace Enset.Api.Authorization;

public static class AuthorizationPolicyNames
{
    public const string Authenticated = nameof(Authenticated);
    public const string EnsetEmployee = nameof(EnsetEmployee);
    public const string EnsetAdmin = nameof(EnsetAdmin);
    public const string CustomerReader = nameof(CustomerReader);
    public const string CustomerWriter = nameof(CustomerWriter);
    public const string CustomerAdmin = nameof(CustomerAdmin);
}
