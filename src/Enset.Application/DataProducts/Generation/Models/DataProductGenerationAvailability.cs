namespace Enset.Application.DataProducts.Generation.Models;

public sealed record DataProductGenerationAvailability(
    bool IsAvailable,
    bool IsAuthorized,
    bool HasRequiredInputData,
    IReadOnlyCollection<string> MissingInputs,
    IReadOnlyCollection<string> Warnings)
{
    public static DataProductGenerationAvailability Available(
        IReadOnlyCollection<string>? warnings = null)
    {
        return new DataProductGenerationAvailability(
            IsAvailable: true,
            IsAuthorized: true,
            HasRequiredInputData: true,
            MissingInputs: Array.Empty<string>(),
            Warnings: warnings ?? Array.Empty<string>());
    }

    public static DataProductGenerationAvailability Unauthorized(
        string reason)
    {
        return new DataProductGenerationAvailability(
            IsAvailable: false,
            IsAuthorized: false,
            HasRequiredInputData: false,
            MissingInputs: new[] { reason },
            Warnings: Array.Empty<string>());
    }

    public static DataProductGenerationAvailability MissingData(
        IReadOnlyCollection<string> missingInputs,
        IReadOnlyCollection<string>? warnings = null)
    {
        return new DataProductGenerationAvailability(
            IsAvailable: false,
            IsAuthorized: true,
            HasRequiredInputData: false,
            MissingInputs: missingInputs,
            Warnings: warnings ?? Array.Empty<string>());
    }

    public static DataProductGenerationAvailability MissingData(
        params string[] missingInputs) =>
        MissingData((IReadOnlyCollection<string>)missingInputs);
}
