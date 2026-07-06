using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class CustomerExcelRowMapper
{
    public static CustomerImportDto ToDto(CustomerExcelRow row)
    {
        var companyName = !string.IsNullOrWhiteSpace(row.OrganizationName)
            ? row.OrganizationName
            : $"{row.FirstName} {row.LastName}".Trim();

        return new CustomerImportDto
        {
            ExternalCustomerId = row.InternalCustomerId,
            CompanyName = companyName,
            Street = row.Street,
            HouseNumber = row.HouseNumber,
            PostalCode = row.PostalCode,
            City = row.City,
            Email = row.Email,
            Phone = row.PhoneNumber
        };
    }
}