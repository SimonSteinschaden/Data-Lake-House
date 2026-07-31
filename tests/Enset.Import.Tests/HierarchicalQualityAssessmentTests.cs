using Enset.Application.Quality;
using Enset.Domain.Curation;
using Xunit;

namespace Enset.Import.Tests;

public sealed class HierarchicalQualityAssessmentTests
{
    private static readonly QualityRequirement[] GoldCore =
    [
        new("TYPE", "Gebäudetyp", QualityRequirementState.Confirmed),
        new("USE", "Nutzungstyp", QualityRequirementState.Confirmed),
        new("STATE", "Gebäudezustand", QualityRequirementState.Confirmed),
        new("POSTAL", "PLZ", QualityRequirementState.Confirmed)
    ];

    [Fact]
    public void Missing_inventory_keeps_building_bronze()
    {
        var result = HierarchicalQualityAssessment.Evaluate(Input(
            meterInventoryComplete: false));
        Assert.Equal(DataMaturityLevel.Bronze, result.OverallQualityLevel);
    }

    [Fact]
    public void Silver_child_limits_building_to_silver()
    {
        var result = HierarchicalQualityAssessment.Evaluate(Input(
            meterLevel: DataMaturityLevel.Silver));
        Assert.Equal(DataMaturityLevel.Silver, result.OverallQualityLevel);
    }

    [Fact]
    public void All_confirmed_gold_scopes_produce_gold()
    {
        var result = HierarchicalQualityAssessment.Evaluate(Input());
        Assert.Equal(DataMaturityLevel.Gold, result.OverallQualityLevel);
        Assert.Equal(100, result.GoldProgress.Percentage);
    }

    [Fact]
    public void No_meter_can_never_produce_gold()
    {
        var input = Input() with { Meters = [] };
        var result = HierarchicalQualityAssessment.Evaluate(input);
        Assert.Equal(DataMaturityLevel.Bronze, result.OverallQualityLevel);
    }

    [Fact]
    public void Progress_exposes_absolute_distribution()
    {
        var progress = HierarchicalQualityAssessment.Progress(
        [
            new("A", "A", QualityRequirementState.Confirmed),
            new("B", "B", QualityRequirementState.Complete),
            new("C", "C", QualityRequirementState.Missing)
        ]);
        Assert.Equal(50, progress.Percentage);
        Assert.Equal((1, 1, 1),
            (progress.GoldCount, progress.SilverCount, progress.BronzeCount));
    }

    private static BuildingQualityInput Input(
        DataMaturityLevel meterLevel = DataMaturityLevel.Gold,
        bool meterInventoryComplete = true) =>
        new(GoldCore,
            [new(Guid.NewGuid(), "Zählpunkt", meterLevel, [])],
            [],
            meterInventoryComplete,
            true,
            true,
            AnnualEnergyStatus.Confirmed,
            AnnualEnergyStatus.Confirmed,
            false);
}
