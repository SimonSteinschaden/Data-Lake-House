using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.Abstractions;

public interface IExcelCustomerReader
{
    IEnumerable<CustomerImportDto> Read(Stream stream);
}