using System.Text.Json;
using Enset.Application.GoldProfiles;
using Enset.Application.InternalDataProducts;
using Xunit;

namespace Enset.Import.Tests;

public sealed class InternalDataProductContractTests
{
    [Fact]
    public void Energy_summary_serializes_group_and_unit_transparently()
    {
        var item = new EnergySummaryItem("Electricity", "Consumption", 42m,
            "KWh", "Manual", null, 2);

        var json = JsonSerializer.Serialize(item);

        Assert.Contains("\"EnergyCarrier\":\"Electricity\"", json);
        Assert.Contains("\"Unit\":\"KWh\"", json);
        Assert.Contains("\"MeterCount\":2", json);
    }

    [Fact]
    public void Missing_gold_profile_is_representable_without_fake_version()
    {
        var gold = new GoldProfileSummary(null, null, "NotAvailable", null);

        Assert.Null(gold.VersionId);
        Assert.Null(gold.VersionNumber);
        Assert.Equal("NotAvailable", gold.ReleaseStatus);
    }

    [Fact]
    public void Readiness_is_explicitly_a_status_not_a_product_result()
    {
        var readiness = new ReadinessSummary(
            DataProductReadinessStatus.NotReady, 25, ["Released profile missing"]);

        Assert.Equal(DataProductReadinessStatus.NotReady, readiness.Status);
        Assert.Single(readiness.BlockingIssues);
    }

    [Fact]
    public void Portfolio_readiness_contract_exposes_counts_blockers_and_actions()
    {
        var summary = new PortfolioReadinessSummary(
            "BuildingBenchmark", DataProductReadinessStatus.NotReady, 40,
            4, 6, ["Keine freigegebene Gold-Profil-Version"],
            ["Gold-Profile freigeben"], "/buildings");

        var json = JsonSerializer.Serialize(summary);

        Assert.Contains("\"ReadyScopeCount\":4", json);
        Assert.Contains("\"BlockedScopeCount\":6", json);
        Assert.Contains("Keine freigegebene Gold-Profil-Version", json);
        Assert.DoesNotContain("BenchmarkResult", json);
    }

    [Fact]
    public void Not_evaluated_portfolio_readiness_has_no_fake_percentage()
    {
        var summary = new PortfolioReadinessSummary(
            "NormalizedLoadProfile", DataProductReadinessStatus.NotReady, null,
            0, 0, [], [], "/meters");

        Assert.Null(summary.Percentage);
        Assert.Empty(summary.TopBlockers);
    }
}
