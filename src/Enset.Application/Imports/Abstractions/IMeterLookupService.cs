namespace Enset.Application.Imports.Abstractions;

public interface IMeterLookupService
{
    Task<Dictionary<string, Guid>> GetMeterLookupAsync(
        CancellationToken cancellationToken = default);
}