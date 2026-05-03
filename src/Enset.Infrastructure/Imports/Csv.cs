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
                Timestamp = DateTime.Parse(parts[0]),
                Value = decimal.Parse(parts[1])
            };
        }
    }
}