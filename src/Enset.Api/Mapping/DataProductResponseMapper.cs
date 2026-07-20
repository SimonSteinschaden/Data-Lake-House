using Enset.Api.Contracts.DataProducts;
using Enset.Domain.DataProducts;

namespace Enset.Api.Mapping;

public static class DataProductResponseMapper
{
    public static DataProductSummaryDto ToSummary(this DataProduct product) =>
        new(product.Id, product.Definition.Code, product.Name,
            product.Definition.Category.ToString(), product.Status.ToString(),
            product.ScopeAssignments.SingleOrDefault()?.ScopeType.ToString() ?? "Unknown",
            ScopeId(product.ScopeAssignments.SingleOrDefault()),
            product.Versions.Count == 0 ? null : product.Versions.Max(x => x.VersionNumber));

    public static LatestDataProductVersionDto ToDto(this DataProductVersion version) =>
        new(version.DataProductId, version.VersionNumber, version.Status.ToString(),
            version.GeneratedAt, version.InputPeriodFrom, version.InputPeriodTo,
            version.Quality.ToString(), version.GenerationRun?.Status.ToString(),
            SplitWarnings(version.GenerationRun?.Warnings),
            version.Values.Select(x => new DataProductValueDto(x.Key,
                x.NumericValue, x.TextValue, x.BooleanValue, x.DateTimeValue,
                x.Unit, x.Quality.ToString())).ToArray());

    private static IReadOnlyCollection<string> SplitWarnings(string? warnings) =>
        string.IsNullOrWhiteSpace(warnings)
            ? Array.Empty<string>()
            : warnings.Split(Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Guid? ScopeId(DataProductScopeAssignment? scope) =>
        scope?.MeterId ?? scope?.BuildingId ?? scope?.EnergySystemId
        ?? scope?.MunicipalityId ?? scope?.RegionId ?? scope?.CustomerId
        ?? scope?.EnergyCommunityId;
}
