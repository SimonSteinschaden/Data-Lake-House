using Enset.Api.Contracts.Common.Responses;
using Enset.Domain.Customers;

namespace Enset.Api.Contracts.Customers.Responses;

public sealed record CustomerListItemResponse(
    Guid Id,
    string CustomerNumber,
    string Name,
    CustomerType Type,
    string? City,
    bool IsActive,
    ObjectMetadataResponse Metadata,
    ValidationMetadataResponse Validation
);
