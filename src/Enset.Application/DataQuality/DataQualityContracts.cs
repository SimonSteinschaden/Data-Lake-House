namespace Enset.Application.QualityManagement;

public sealed record QualityMetric(
    string Code, string Name, string Description, string Severity, long Count,
    string Trend, int AffectedCustomers, int AffectedBuildings,
    int AffectedMeters, string Action, string ActionUrl);

public sealed record QualityDistribution(string Level, int Count);
public sealed record SuitabilityMetric(string UseCase, int Suitable, int NotSuitable);
public sealed record DataQualityDashboard(
    DateTime CalculatedAtUtc, int CustomerCompleteness, int BuildingCompleteness,
    int MeterCompleteness, IReadOnlyList<QualityMetric> Metrics,
    IReadOnlyList<QualityDistribution> QualityLevels,
    IReadOnlyList<SuitabilityMetric> Suitability,
    int OpenImportIssues, int OpenDataReviews);
public sealed record QualityTrendPoint(DateOnly Date, long Count);

public interface IDataQualityDashboardService
{
    Task<DataQualityDashboard> Get(CancellationToken cancellationToken);
    Task<QualityMetric?> GetMetric(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<QualityTrendPoint>> GetTrend(string code, int days,
        CancellationToken cancellationToken);
}
