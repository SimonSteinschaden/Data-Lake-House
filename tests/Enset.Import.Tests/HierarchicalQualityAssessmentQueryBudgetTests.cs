using Xunit;

namespace Enset.Import.Tests;

public sealed class HierarchicalQualityAssessmentQueryBudgetTests
{
    [Fact]
    public void AssessMeters_uses_batch_queries_without_per_item_loops()
    {
        var body = MethodBody("AssessMeters");
        Assert.Contains("Where(x => set.Contains(x.Id))", body);
        Assert.DoesNotContain("foreach", body);
    }

    [Fact]
    public void AssessEnergySystems_uses_batch_queries_without_per_item_loops()
    {
        var body = MethodBody("AssessEnergySystems");
        Assert.Contains("Where(x => set.Contains(x.Id))", body);
        Assert.DoesNotContain("foreach", body);
    }

    [Fact]
    public void AssessBuildings_uses_batch_queries_without_per_item_loops()
    {
        var body = MethodBody("AssessBuildings");
        Assert.Contains("Where(x => set.Contains(x.Id))", body);
        Assert.DoesNotContain("foreach", body);
        // Child assessments must be resolved via the batched AssessMeters/AssessEnergySystems
        // calls, not via a single-id lookup per meter or energy system.
        Assert.Contains("await AssessMeters(", body);
        Assert.Contains("await AssessEnergySystems(", body);
    }

    private static string MethodBody(string methodName)
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Infrastructure", "Quality",
            "EfHierarchicalQualityAssessmentService.cs"));
        var methodStart = source.IndexOf($" {methodName}(", StringComparison.Ordinal);
        Assert.True(methodStart > 0, $"Method {methodName} not found.");
        var braceStart = source.IndexOf('{', methodStart);
        var depth = 0;
        var index = braceStart;
        for (; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0) break;
            }
        }
        return source[braceStart..(index + 1)];
    }

    private static string FindSource(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            directory = directory.Parent;
        if (directory is null)
            throw new DirectoryNotFoundException("Repository root not found.");
        return Path.Combine([directory.FullName, .. segments]);
    }
}
