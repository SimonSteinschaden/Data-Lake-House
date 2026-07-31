using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Abstractions;

public interface IImportMapper
{
    IReadOnlyList<CustomerImportDto> Map(
        IReadOnlyCollection<CustomerExcelRow> rows);
}
