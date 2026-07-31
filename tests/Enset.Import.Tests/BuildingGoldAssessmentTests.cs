using Enset.Application.CanonicalSnapshots;
using Enset.Domain.Curation;
using Xunit;

namespace Enset.Import.Tests;

public sealed class BuildingGoldAssessmentTests
{
    [Theory]
    [InlineData(null, null, null, null, 0)]
    [InlineData("House", null, null, null, 25)]
    [InlineData("House", "Residential", null, null, 50)]
    [InlineData("House", "Residential", "Existing", null, 75)]
    [InlineData("House", "Residential", "Existing", "1090", 100)]
    public void Readiness_UsesTheFourCentralBusinessFields(
        string? category,
        string? usage,
        string? state,
        string? postalCode,
        int expected)
    {
        var result = BuildingGoldDefinition.Evaluate(
            category,
            usage,
            state,
            postalCode);

        Assert.Equal(expected, result.GoldCompletenessPercentage);
        Assert.Equal(4, result.GoldRequiredFieldCount);
        Assert.Equal(expected / 25, result.GoldPresentFieldCount);
        Assert.Equal(
            4 - expected / 25,
            result.MissingReasons.Count);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("NotSpecified")]
    [InlineData("")]
    [InlineData("   ")]
    public void TechnicalEmptyValues_AreNotComplete(string value)
    {
        var result = BuildingGoldDefinition.Evaluate(
            value,
            value,
            value,
            value);

        Assert.Equal(0, result.GoldCompletenessPercentage);
        Assert.All(result.GoldFieldStates, item => Assert.False(item.HasValue));
    }

    [Fact]
    public void PresentAndConfirmed_AreEvaluatedSeparately()
    {
        var result = BuildingGoldDefinition.Evaluate(
            "House",
            "Residential",
            "Existing",
            "1090",
            new Dictionary<string, bool>
            {
                ["BuildingCategory"] = true,
                ["PrimaryUseType"] = false,
                ["BuildingState"] = false,
                ["PostalCode"] = false
            });

        Assert.Equal(100, result.GoldCompletenessPercentage);
        Assert.Equal(25, result.GoldConfirmationPercentage);
        Assert.Equal(DataMaturityLevel.Silver, result.MaturityLevel);
        Assert.Empty(result.MissingReasons);
        Assert.Equal(3, result.ConfirmationReasons.Count);
        Assert.Equal(4, result.GoldPresentFieldCount);
        Assert.Equal(1, result.GoldConfirmedFieldCount);
        Assert.DoesNotContain(result.GoldFieldStates, item =>
            item.FieldName is "CustomerId" or "BuildingId");
        Assert.Equal(
            BuildingGoldFieldState.PresentUnconfirmed,
            result.GoldFieldStates.Single(item =>
                item.FieldName == "PrimaryUseType").State);
        Assert.Contains(
            "Nutzungstyp ist vorhanden, aber noch nicht fachlich bestätigt. Aktueller Wert: Residential.",
            result.ConfirmationReasons);
    }

    [Fact]
    public void FullyConfirmedValues_AreGoldReady()
    {
        var confirmations = new Dictionary<string, bool>
        {
            ["BuildingCategory"] = true,
            ["PrimaryUseType"] = true,
            ["BuildingState"] = true,
            ["PostalCode"] = true
        };

        var result = BuildingGoldDefinition.Evaluate(
            "House",
            "Residential",
            "Existing",
            "1090",
            confirmations);

        Assert.True(result.IsGoldReady);
        Assert.Equal(100, result.GoldCompletenessPercentage);
        Assert.Equal(100, result.GoldConfirmationPercentage);
        Assert.Equal(DataMaturityLevel.Gold, result.MaturityLevel);
        Assert.Empty(result.UnfulfilledReasons);
    }
}
