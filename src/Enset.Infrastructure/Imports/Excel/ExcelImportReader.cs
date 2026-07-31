using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Excel;

public sealed class ExcelImportReader : IImportReader
{
    private readonly IExcelWorkbookReader _workbookReader;
    private readonly string _filePath;

    public ExcelImportReader(
        IExcelWorkbookReader workbookReader,
        string filePath)
    {
        _workbookReader = workbookReader;
        _filePath = filePath;
    }

    public ImportWorkbook Read()
    {
        using var stream = File.OpenRead(_filePath);

        return _workbookReader.Read(stream);
    }
}