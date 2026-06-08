using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Abstractions;

public interface IExcelReader
{
    IReadOnlyList<CustomerExcelRow> ReadCustomers(string filePath);

    IReadOnlyList<BuildingExcelRow> ReadBuildings(string filePath);
}