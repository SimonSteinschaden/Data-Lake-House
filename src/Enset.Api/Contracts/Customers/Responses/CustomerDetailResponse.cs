using Enset.Api.Contracts.Common.Responses;
using Enset.Domain.Customers;

namespace Enset.Api.Contracts.Customers.Responses;

public sealed record CustomerDetailResponse(
    Guid Id,

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
    string CountryCode,

    bool IsActive,

    ObjectMetadataResponse Metadata,
    ValidationMetadataResponse Validation,

    IReadOnlyList<SampleReferenceResponse> SampleReferences
);