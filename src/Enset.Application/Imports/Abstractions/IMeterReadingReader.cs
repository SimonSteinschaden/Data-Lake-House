using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.Abstractions;

public interface IMeterReadingReader
{
    IEnumerable<MeterReadingImportDto> Read(Stream stream);
}