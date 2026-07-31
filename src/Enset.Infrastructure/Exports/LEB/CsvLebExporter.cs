using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Models;

namespace Enset.Infrastructure.Exports.LEB;

public sealed class CsvLebExporter : ICsvLebExporter
{
    public LebExportFile Export(NoeLebExportContractV1 contract)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Add(archive, "Municipalities.csv", contract.Municipalities);
            Add(archive, "Objects.csv", contract.Objects);
            Add(archive, "Meters.csv", contract.Meters);
            Add(archive, "Readings.csv", contract.Readings);
            Add(archive, "EnergySystems.csv", contract.EnergySystems);
        }
        return new(output.ToArray(), "application/zip",
            $"NoeLebExport_{contract.ExportTimestamp:yyyyMMdd_HHmmss}.zip");
    }

    private static void Add<T>(ZipArchive archive, string name, IReadOnlyList<T> rows)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        writer.WriteLine(string.Join(';', properties.Select(x => Escape(x.Name))));
        foreach (var row in rows)
            writer.WriteLine(string.Join(';', properties.Select(x =>
                Escape(Format(x.GetValue(row))))));
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime timestamp => timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        bool boolean => boolean ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
    private static string Escape(string value) => value.IndexOfAny([';', '"', '\r', '\n']) < 0
        ? value : $"\"{value.Replace("\"", "\"\"")}\"";
}
