public interface IMeterReadingReaderFactory
{
    IMeterReadingReader Create(ImportSourceType type);
}