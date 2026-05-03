using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Abstractions;

public interface IMeterReadingReaderFactory
{
    IMeterReadingReader Create(ImportSourceType type);
}