using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public sealed class CustomerImportMapper : IImportMapper
{
    public IReadOnlyList<CustomerImportDto> Map(
        IReadOnlyCollection<CustomerExcelRow> rows)
    {
        return rows.Select(CustomerExcelRowMapper.ToDto).ToList();
    }
}
