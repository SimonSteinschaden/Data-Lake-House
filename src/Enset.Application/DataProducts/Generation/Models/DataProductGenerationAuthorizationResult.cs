namespace Enset.Application.DataProducts.Generation.Models;

public sealed record DataProductGenerationAuthorizationResult(
    bool IsAuthorized,
    string? DenialReason = null)
{
    public static DataProductGenerationAuthorizationResult Allowed()
    {
        return new DataProductGenerationAuthorizationResult(
            IsAuthorized: true);
    }

    public static DataProductGenerationAuthorizationResult Denied(
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new DataProductGenerationAuthorizationResult(
            IsAuthorized: false,
            DenialReason: reason);
    }
}