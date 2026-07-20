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
            VatNumber = row.VatNumber,
            CompanyRegistrationNumber = row.CompanyRegistrationNumber,
            ContactPerson = row.ContactPerson,
            Street = row.Street,
            HouseNumber = row.HouseNumber,
            PostalCode = row.PostalCode,
            City = row.City,
            Country = row.Country,
            Email = row.Email,
            Phone = row.PhoneNumber
        };
    }
}
