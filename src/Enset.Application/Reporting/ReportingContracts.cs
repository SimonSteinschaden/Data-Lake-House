using Enset.Application.ObjectAnalytics;

namespace Enset.Application.Reporting;

public enum ReportType
{
    ObjectEnergy,
    AnnualEnergy,
    Consumption,
    Cost,
    Co2,
    LoadProfile,
    EnergySystems,
    DataQuality,
    PortfolioComparison,
    Iso50001,
    Landesenergiebuchhaltung
}

public enum ReportReleaseStatus
{
    Draft,
    Released,
    Archived
}

public sealed record ReportDefinition(
    ReportType Type,
    string Title,
    string Description,
    IReadOnlyList<string> SupportedFormats);

public sealed record CreateReportRequest(
    ReportType Type,
    Guid BuildingId,
    DateTime FromUtc,
    DateTime ToUtc,
    string Recipient);

public sealed record ReportInstance(
    Guid ReportId,
    ReportType Type,
    Guid BuildingId,
    string BuildingName,
    DateTime FromUtc,
    DateTime ToUtc,
    int Version,
    DateTime CreatedAtUtc,
    string Recipient,
    ReportReleaseStatus ReleaseStatus,
    string QualityLevel,
    string Suitability,
    ObjectAnalyticsProduct Product)
{
    public int GoldProgressPercentage { get; init; }
    public int BronzeCount { get; init; }
    public int SilverCount { get; init; }
    public int GoldCount { get; init; }
    public string InventoryDeclarationStatus { get; init; } = "Nicht bestätigt";
    public IReadOnlyList<string> AnalysisVersions { get; init; } = [];
    public int OpenIssueCount { get; init; }
    public IReadOnlyList<string> BlockingReasons { get; init; } = [];
    public string ConfirmationStatus { get; init; } = "Nicht bestätigt";
    public string? ReleasedBy { get; init; }
    public DateTime? ReleasedAtUtc { get; init; }
}

public sealed record RenderedReport(
    string FileName,
    string ContentType,
    byte[] Content);

public interface IReportService
{
    IReadOnlyList<ReportDefinition> Definitions();
    Task<IReadOnlyList<ReportInstance>> List(
        CancellationToken cancellationToken);
    Task<ReportInstance> Create(
        CreateReportRequest request,
        CancellationToken cancellationToken);
    Task<ReportInstance?> Get(
        Guid reportId,
        CancellationToken cancellationToken);
    Task<RenderedReport?> Export(
        Guid reportId,
        string format,
        CancellationToken cancellationToken);
    Task<ReportInstance?> Release(
        Guid reportId,
        string releasedBy,
        CancellationToken cancellationToken);
    Task<ReportInstance?> Archive(
        Guid reportId,
        CancellationToken cancellationToken);
}
