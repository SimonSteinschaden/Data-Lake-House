using System.Globalization;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;

namespace Enset.Infrastructure.Imports.Readers;

public class CsvMeterReadingReader : IMeterReadingReader
{
    public IEnumerable<MeterReadingImportDto> Read(Stream stream)
    {
        using var reader = new StreamReader(stream);

        reader.ReadLine(); // Header skippen

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(';');

            if (parts.Length < 2)
                continue;

            if (!DateTime.TryParse(parts[0], out var timestamp))
                continue;

            if (!decimal.TryParse(
                    parts[1],
                    NumberStyles.Any,
                    new CultureInfo("de-DE"),
                    out var value))
            {
                value = 0m;
            }

            yield return new MeterReadingImportDto
            {
                Timestamp = timestamp,
                Value = value
            };
        }
    }
}