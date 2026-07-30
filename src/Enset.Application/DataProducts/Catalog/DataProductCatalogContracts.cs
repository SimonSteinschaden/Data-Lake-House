using Enset.Application.CanonicalSnapshots;

namespace Enset.Application.DataProducts.Catalog;

public sealed record SemanticVersion(int Major, int Minor, int Patch)
{
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

public sealed record DataProductMetadata(
    string Code, string Name, string GermanName, string Description,
    string Category, SemanticVersion Version, string Owner,
    IReadOnlyList<string> Inputs, IReadOnlyList<string> UsedProducts,
    IReadOnlyList<string> OutputSchema, string DataSource,
    string SnapshotVersion, string QualityLevel, string Suitability,
    string Refresh, IReadOnlyList<string> SupportedExports,
    string ApiEndpoint, string Period, string AggregationLevel,
    string MissingDataBehavior, string Lineage);

public sealed record DataProductCatalogItem(
    DataProductMetadata Metadata, DateTime LastUpdatedUtc);

public sealed record DataProductPreview(
    DataProductMetadata Metadata, DateTime GeneratedAtUtc,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record DataProductDependency(string Product, IReadOnlyList<string> DependsOn);

public sealed record DataProductExport(string FileName, string ContentType, byte[] Content);

public interface IDataProductCatalogService
{
    IReadOnlyList<DataProductCatalogItem> List(string? search = null, string? category = null);
    DataProductCatalogItem? Get(string code);
    IReadOnlyList<DataProductDependency> Dependencies();
    Task<DataProductPreview?> Preview(string code, Guid? customerId, Guid? buildingId,
        DateTime? fromUtc, DateTime? toUtc, int limit, CancellationToken cancellationToken);
    Task<DataProductExport?> Export(string code, string format, Guid? customerId,
        Guid? buildingId, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken);
}
