using System.Text.Json;
using Enset.Application.Imports.Enums;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportSourceTypeTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void CRMExcelIsWrittenWithNewSourceTypeName()
    {
        var json = JsonSerializer.Serialize(
            ImportSourceType.CRM_Excel,
            Options);

        Assert.Equal("\"CRM_Excel\"", json);
    }

    [Fact]
    public void LegacyWorkbookSourceTypeRemainsReadable()
    {
        var sourceType = JsonSerializer.Deserialize<ImportSourceType>(
            "\"EnsetWorkbook\"",
            Options);

        Assert.Equal(ImportSourceType.CRM_Excel, sourceType);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ImportSourceTypeJsonConverter());
        return options;
    }
}
