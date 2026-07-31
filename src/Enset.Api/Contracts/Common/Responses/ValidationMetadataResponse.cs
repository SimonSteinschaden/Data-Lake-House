namespace Enset.Api.Contracts.Common.Responses;

public sealed record ValidationMetadataResponse(
    string ValidationState,
    bool IsReferenceOutdated,
    DateTime? LastValidatedAt,
    string? LastValidatedBy
);