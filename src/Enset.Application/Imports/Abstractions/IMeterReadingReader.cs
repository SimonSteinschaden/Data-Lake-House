public interface IMeterReadingReader
{
    IEnumerable<MeterReadingImportDto> Read(Stream stream);
}