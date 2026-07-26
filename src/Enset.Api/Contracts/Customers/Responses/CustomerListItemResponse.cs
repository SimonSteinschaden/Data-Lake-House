using Enset.Api.Contracts.Common.Responses;

namespace Enset.Api.Contracts.Customers.Responses;

public sealed record CustomerListItemResponse(
    Guid Id,
    string InternalCustomerId,
    string Name,
    string? City,
    ObjectMetadataResponse Metadata,
    ValidationMetadataResponse Validation
);