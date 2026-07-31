using Enset.Application.Imports.DTOs;

namespace Enset.Infrastructure.Imports.Excel;

public interface IExcelWorkbookWriter
{
    void UpdateCustomers(IEnumerable<CustomerImportDto> customers);

    // TODO:
    // void UpdateBuildings(...);
    // void UpdateMeters(...);
}