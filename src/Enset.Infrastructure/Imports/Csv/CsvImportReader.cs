using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Readers;

public sealed class CsvImportReader : IImportReader
{
    private readonly string _filePath;
    private readonly CsvMeterReadingReader _reader;

    public CsvImportReader(string filePath, CsvMeterReadingReader reader)
    {
        _filePath = filePath;
        _reader = reader;
    }

    public ImportWorkbook Read()
    {
        using var stream = File.OpenRead(_filePath);
        var mapping = _reader.ReadMapping(stream);
        return new ImportWorkbook
        {
            SourceType = ImportSourceType.Csv,
            CsvMapping = mapping,
            MeterReadings = CsvMeterReadingMappingService.Map(
                mapping,
                _reader.DefaultMeterNumber)
        };
    }
}
