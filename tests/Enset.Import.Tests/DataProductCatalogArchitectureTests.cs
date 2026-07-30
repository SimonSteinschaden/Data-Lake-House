using Enset.Infrastructure.DataProducts;
using Xunit;

namespace Enset.Import.Tests;

public sealed class DataProductCatalogArchitectureTests
{
    private readonly CanonicalDataProductCatalogService service =
        new(null!, TimeProvider.System);

    [Fact]
    public void Catalog_contains_documented_versioned_products()
    {
        var products = service.List();
        Assert.True(products.Count >= 18);
        Assert.All(products, x => Assert.True(
            x.Metadata.Version.Major >= 1 &&
            x.Metadata.Inputs.Count > 0 &&
            x.Metadata.OutputSchema.Count > 0 &&
            x.Metadata.SupportedExports.Contains("json") &&
            x.Metadata.SupportedExports.Contains("csv") &&
            x.Metadata.SupportedExports.Contains("xlsx")));
    }

    [Fact]
    public void Existing_products_are_part_of_the_central_catalog()
    {
        Assert.NotNull(service.Get("BUILDING_ENERGY_PROFILE"));
        Assert.NotNull(service.Get("METER_CONSUMPTION_SUMMARY"));
    }

    [Fact]
    public void Dependency_graph_is_acyclic()
    {
        var graph = service.Dependencies().ToDictionary(x => x.Product, x => x.DependsOn);
        foreach (var product in graph.Keys)
            Assert.False(HasCycle(product, product, graph, []));
    }

    private static bool HasCycle(string root, string current,
        IReadOnlyDictionary<string, IReadOnlyList<string>> graph, HashSet<string> visited)
    {
        if (!graph.TryGetValue(current, out var dependencies)) return false;
        foreach (var dependency in dependencies.Where(graph.ContainsKey))
        {
            if (dependency == root) return true;
            if (visited.Add(dependency) && HasCycle(root, dependency, graph, visited))
                return true;
        }
        return false;
    }
}
