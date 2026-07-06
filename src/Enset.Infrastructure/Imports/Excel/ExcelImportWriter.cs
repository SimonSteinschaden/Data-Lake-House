using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.WriteGate;

namespace Enset.Infrastructure.Imports.Excel;

public sealed class ExcelImportWriter : IImportWriter
{
    private readonly IExcelWorkbookWriter _workbookWriter;

    public ExcelImportWriter(IExcelWorkbookWriter workbookWriter)
    {
        _workbookWriter = workbookWriter;
    }

    public void Write(ImportWriteContext context)
    {
        _workbookWriter.UpdateCustomers(context.Customers);
    }
}
