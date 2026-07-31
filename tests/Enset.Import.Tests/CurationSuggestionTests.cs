using Enset.Infrastructure.Curation;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CurationSuggestionTests
{
    [Fact]
    public void School_name_produces_explainable_high_confidence_suggestions()
    {
        var category = EfCurationService.SuggestBuildingCategory("Volksschule Mitte");
        var usage = EfCurationService.SuggestUsage("Volksschule Mitte");

        Assert.Equal("School", category.Value);
        Assert.True(category.Confidence >= 95);
        Assert.Contains("Schule", category.Reason);
        Assert.Equal("Public", usage.Value);
        Assert.True(usage.Confidence >= 90);
    }

    [Fact]
    public void Meter_medium_uses_visible_name_evidence()
    {
        var suggestion = EfCurationService.SuggestMedium(
            "Strom Hauptzähler", "kWh", "Energy");

        Assert.Equal("Electricity", suggestion.Value);
        Assert.Equal(96, suggestion.Confidence);
        Assert.Contains("Strom", suggestion.Reason);
    }

    [Fact]
    public void Ambiguous_values_are_marked_with_low_confidence()
    {
        var suggestion = EfCurationService.SuggestBuildingCategory("Objekt 17");

        Assert.Equal("Other", suggestion.Value);
        Assert.True(suggestion.Confidence < 60);
        Assert.NotEmpty(suggestion.Reason);
    }
}
