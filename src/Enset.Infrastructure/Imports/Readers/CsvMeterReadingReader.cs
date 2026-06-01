using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Abstractions;

namespace Enset.Infrastructure.Imports;

public class CsvMeterReadingReader : IMeterReadingReader
{
    public IEnumerable<MeterReadingImportDto> Read(Stream stream)
    {
        using var reader = new StreamReader(stream);

        // Beispiel minimal – später robust machen
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(';'); // oder ','

            yield return new MeterReadingImportDto
            {
                MeterNumber = parts[0],
                Timestamp = DateTime.Parse(parts[1]),
                Value = decimal.Parse(parts[2]),
                Unit = parts.Length > 3 ? parts[3] : null,
                QualityFlag = parts.Length > 4 ? int.Parse(parts[4]) : null,
                BuildingId = parts.Length > 5 && Guid.TryParse(parts[5], out var b) ? b : null,
                CustomerId = parts.Length > 6 && Guid.TryParse(parts[6], out var c) ? c : null
            };
        }
    }
}