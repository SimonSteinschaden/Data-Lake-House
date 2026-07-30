using Xunit;

namespace Enset.Import.Tests;

public sealed class UnifiedCrudArchitectureTests
{
    [Fact]
    public void CrudReadService_UsesCanonicalSnapshotsWithoutParallelRules()
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Infrastructure", "ReadModel",
            "EfEntityReadService.cs"));

        Assert.Contains("ICanonicalSnapshotReader snapshots", source);
        Assert.DoesNotContain("CuratedFieldValues", source);
        Assert.DoesNotContain("CanonicalAnnualValue.Calculate", source);
        Assert.DoesNotContain("TimeSpan.FromDays(364", source);
    }

    [Fact]
    public void FrontendDisplays_DoNotCalculateAnnualValuesOrQuality()
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Web", "src", "components", "domain",
            "CanonicalDisplays.tsx"));

        Assert.DoesNotContain("reduce(", source);
        Assert.DoesNotContain("goldMaturityPercentage", source);
        Assert.Contains("status === \"IncompleteYear\"", source);
        Assert.Contains("level || \"–\"", source);
    }

    private static string FindSource(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                [directory.FullName, .. segments]);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException(Path.Combine(segments));
    }
}
