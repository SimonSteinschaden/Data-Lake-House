using Enset.Application.Curation;
using Enset.Domain.Curation;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CurationMaturityTests
{
    [Fact]
    public void Field_maturity_supports_partial_bronze_silver_and_gold()
    {
        var levels = new[] { DataMaturityLevel.Gold, DataMaturityLevel.Silver, DataMaturityLevel.Bronze };
        Assert.Contains(DataMaturityLevel.Gold, levels);
        Assert.Contains(DataMaturityLevel.Silver, levels);
        Assert.Contains(DataMaturityLevel.Bronze, levels);
    }

    [Fact]
    public void Complete_fifteen_minute_period_is_one_hundred_percent()
    {
        var start = new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc);
        var timestamps = Enumerable.Range(0, 96).Select(i => start.AddMinutes(i * 15));
        var result = TimeSeriesCompletenessCalculator.Calculate(timestamps, 15);
        Assert.Equal(96, result.Expected);
        Assert.Equal(100m, result.CompletenessPercentage);
    }

    [Fact]
    public void Missing_and_duplicate_intervals_are_reported_separately()
    {
        var start = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc);
        var timestamps = new[] { start, start, start.AddMinutes(30), start.AddMinutes(45) };
        var result = TimeSeriesCompletenessCalculator.Calculate(timestamps, 15);
        Assert.Equal(4, result.Expected);
        Assert.Equal(1, result.Missing);
        Assert.Equal(1, result.DuplicateTimestamps);
    }

    [Fact]
    public void Invalid_interval_is_rejected()
    {
        Assert.Throws<CurationValidationException>(() =>
            TimeSeriesCompletenessCalculator.Calculate([], 0));
    }
}
