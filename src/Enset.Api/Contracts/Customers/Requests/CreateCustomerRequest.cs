using Enset.Domain.Customers;

namespace Enset.Api.Contracts.Customers.Requests;

public sealed record CreateCustomerRequest(
    string CustomerNumber,
    string Name,
    string? LegalName,
    CustomerType Type,

    string? CompanyRegistrationNumber,
    string? VatIdentificationNumber,

    string? Email,
    string? Phone,
    string? Website,

    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City,
    string CountryCode
);
