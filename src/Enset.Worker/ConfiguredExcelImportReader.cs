using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Models;

namespace Enset.Worker;

public sealed class ConfiguredExcelImportReader : IImportReader
{
    private readonly IExcelReader _reader;
    private readonly string _filePath;

    public ConfiguredExcelImportReader(IExcelReader reader, string filePath)
    {
        _reader = reader;
        _filePath = filePath;
    }

    public ImportWorkbook Read()
    {
        return new ImportWorkbook
        {
            Customers = _reader.ReadCustomers(_filePath),
            Buildings = _reader.ReadBuildings(_filePath)
        };
    }
}
