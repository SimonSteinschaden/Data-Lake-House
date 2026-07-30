using Xunit;

namespace Enset.Import.Tests;

public sealed class LebExportArchitectureTests
{
    [Fact]
    public void Leb_projection_depends_on_canonical_reader_not_ef()
    {
        var source = Read("src", "Enset.Infrastructure", "Exports", "LEB",
            "EfNoeLebContractBuilder.cs");

        Assert.Contains("ICanonicalSnapshotReader snapshots", source);
        Assert.DoesNotContain("EnsetDbContext", source);
        Assert.DoesNotContain("CuratedFieldValues", source);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source);
        Assert.DoesNotContain("CanonicalAnnualValue", source);
    }

    [Fact]
    public void Both_serializers_only_consume_contract()
    {
        foreach (var file in new[] { "CsvLebExporter.cs", "ExcelLebExporter.cs" })
        {
            var source = Read("src", "Enset.Infrastructure", "Exports", "LEB", file);
            Assert.Contains("NoeLebExportContractV1", source);
            Assert.DoesNotContain("EnsetDbContext", source);
            Assert.DoesNotContain("ICanonicalSnapshotReader", source);
        }
    }

    private static string Read(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(path))
                return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new FileNotFoundException(Path.Combine(segments));
    }
}
