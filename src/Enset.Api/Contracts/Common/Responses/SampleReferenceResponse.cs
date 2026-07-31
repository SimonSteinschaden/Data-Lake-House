namespace Enset.Api.Contracts.Common.Responses;

public sealed record SampleReferenceResponse(
    string ReferenceType,
    string? ValidationRule,
    string? Context,
    DateTime ValidatedAt,
    string? ValidatedBy,
    string? Comment,
    bool IsActive,
    bool IsOutdated
);