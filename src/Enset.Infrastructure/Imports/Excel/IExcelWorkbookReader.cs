using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Excel;

public interface IExcelWorkbookReader
{
    ImportWorkbook Read(Stream stream);
}