using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enset.Application.Imports.Enums;

public sealed class ImportSourceTypeJsonConverter
    : JsonConverter<ImportSourceType>
{
    private const string LegacyCRMExcelName = "EnsetWorkbook";

    public override ImportSourceType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number &&
            reader.TryGetInt32(out var numericValue) &&
            Enum.IsDefined(typeof(ImportSourceType), numericValue))
        {
            return (ImportSourceType)numericValue;
        }

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Import source type must be a string.");

        var value = reader.GetString();
        if (string.Equals(
                value,
                LegacyCRMExcelName,
                StringComparison.OrdinalIgnoreCase))
        {
            return ImportSourceType.CRM_Excel;
        }

        if (Enum.TryParse<ImportSourceType>(
                value,
                ignoreCase: true,
                out var sourceType))
        {
            return sourceType;
        }

        throw new JsonException($"Unknown import source type '{value}'.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        ImportSourceType value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
