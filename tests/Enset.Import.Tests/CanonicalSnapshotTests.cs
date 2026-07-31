using Enset.Application.CanonicalSnapshots;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CanonicalSnapshotTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(11)]
    public void IncompleteYearDoesNotExposeAnnualValue(int months)
    {
        var readings = Enumerable.Range(1, months)
            .Select(month => (
                new DateTime(2025, month, 1, 0, 0, 0, DateTimeKind.Utc),
                10m))
            .ToArray();

        var result = CanonicalAnnualValue.Evaluate(
            readings,
            MeterReadingType.IntervalValue);

        Assert.Null(result.Value);
        Assert.Equal(AnnualValueStatus.IncompleteYear, result.Status);
    }

    [Fact]
    public void CompleteTwelveMonthSeriesExposesAnnualValue()
    {
        var readings = Enumerable.Range(1, 12)
            .Select(month => (
                new DateTime(2025, month, 1, 0, 0, 0, DateTimeKind.Utc),
                10m))
            .ToArray();

        var result = CanonicalAnnualValue.Evaluate(
            readings,
            MeterReadingType.IntervalValue);

        Assert.Equal(120m, result.Value);
        Assert.Equal(AnnualValueStatus.CompleteYear, result.Status);
    }

    [Fact]
    public void MissingSeriesHasNotAvailableStatus()
    {
        var result = CanonicalAnnualValue.Evaluate(
            [],
            MeterReadingType.IntervalValue);

        Assert.Null(result.Value);
        Assert.Equal(AnnualValueStatus.NotAvailable, result.Status);
    }

    [Fact]
    public void QualityAndSuitabilityAreIndependentValues()
    {
        var quality = new SnapshotQuality(
            DataMaturityLevel.Silver,
            80,
            100,
            100,
            50);
        var suitability = new SnapshotSuitability(
            SuitabilityStatus.Suitable,
            SuitabilityStatus.NotSuitable,
            SuitabilityStatus.Suitable,
            SuitabilityStatus.NotSuitable);

        Assert.Equal(DataMaturityLevel.Silver, quality.Level);
        Assert.Equal(SuitabilityStatus.Suitable, suitability.Leb);
        Assert.Equal(
            SuitabilityStatus.NotSuitable,
            suitability.Navigator);
    }

    [Fact]
    public void InternalProductServiceHasNoDirectDomainProjection()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Enset.Infrastructure",
            "InternalDataProducts",
            "EfInternalDataProductService.cs"));

        Assert.DoesNotContain("db.Buildings", source);
        Assert.DoesNotContain("db.Customers", source);
        Assert.DoesNotContain("db.Meters", source);
        Assert.DoesNotContain("db.MeterReadings", source);
        Assert.DoesNotContain("db.EnergySystems", source);
        Assert.DoesNotContain("db.CuratedFieldValues", source);
        Assert.Contains("ICanonicalSnapshotReader", source);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ??
               throw new DirectoryNotFoundException(
                   "Repository root not found.");
    }
}
