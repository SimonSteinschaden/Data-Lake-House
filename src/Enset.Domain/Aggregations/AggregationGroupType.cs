namespace Enset.Domain.Aggregation;

/// <summary>
/// Describes logical aggregation groups for assets and time series.
///
/// TODO (Phase 5+):
/// Introduce AggregationGroup and AggregationGroupMember entities.
/// They will enable spatial, organizational and analytical grouping
/// of Buildings, EnergySystems and other future Assets.
/// Examples:
/// - Postal code areas
/// - Municipalities
/// - Regions
/// - Energy communities
/// - Customer portfolios
/// - Grid areas
/// - Analysis scenarios
/// </summary>
/// 
public enum AggregationGroupType
{
    Unknown = 0,

    // Geographic
    PostalCodeArea = 1,
    Municipality = 2,
    District = 3,
    Region = 4,
    Quarter = 5,

    // Organizational
    CustomerPortfolio = 20,
    EnergyCommunity = 21,
    GridArea = 22,

    // Analytical
    AnalysisScenario = 40,

    // Extension point
    Custom = 99
}