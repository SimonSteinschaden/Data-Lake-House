namespace Enset.Application.Imports.Services;

public static class CustomerIdGenerator
{
    public static string Generate()
    {
        return $"CUST-{Guid.NewGuid():N}";
    }
}