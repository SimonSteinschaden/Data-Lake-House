namespace Enset.Application.DataProducts.Commands.GenerateDataProduct;

public sealed record GenerateDataProductCommand(
    Guid DataProductId,
    Guid CustomerId,
    Guid RequestedByUserId,
    DateTime? PeriodFrom = null,
    DateTime? PeriodTo = null,
    IReadOnlyDictionary<string, string>? Parameters = null);