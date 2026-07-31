using ClosedXML.Excel;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Abstractions;

namespace Enset.Infrastructure.Imports.Excel;

public class ExcelCustomerReader : IExcelCustomerReader
{
    public IEnumerable<CustomerImportDto> Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheet("Customers");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            yield return new CustomerImportDto //TODO: SQL Injection Prevention und  Automatisches Mapping: Implementieren Sie eine Validierung und Bereinigung der Eingabedaten, um SQL-Injection-Angriffe zu verhindern. Stellen Sie sicher, dass die Daten korrekt gemappt werden, um die Integrität der importierten Kundendaten zu gewährleisten.
            {
                CompanyName = worksheet.Cell(row, 1).GetString(),
                PostalCode = worksheet.Cell(row, 2).GetString(),
                City = worksheet.Cell(row, 3).GetString(),
                Street = worksheet.Cell(row, 4).GetString(),
                HouseNumber = worksheet.Cell(row, 5).GetString()
            };
        }
    }
}