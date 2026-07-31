using Enset.Domain.Common;

public class EnergyPerformanceAssessment : BaseEntity
{
    public Guid BuildingId { get; set; }

    public Guid? BuildingVersionId { get; set; }

    public string Methodology { get; set; } = string.Empty;

    public string RegulatoryFramework { get; set; } = string.Empty;

    public string RegulatoryVersion { get; set; } = string.Empty;

    public DateTime AssessmentDate { get; set; }

    public DateTime? ValidUntil { get; set; }

    public decimal? HeatingDemandKwhPerM2Year { get; set; }

    public decimal? PrimaryEnergyDemandKwhPerM2Year { get; set; }

    public decimal? CarbonEmissionsKgPerM2Year { get; set; }

    public string? EnergyClass { get; set; }
}