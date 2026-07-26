namespace Enset.Api.Contracts.Common.Responses;

public sealed record ObjectMetadataResponse(
    string Origin,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy,
    string? ChangeSource
);