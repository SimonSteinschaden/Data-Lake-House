using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Domain.Data;
using Enset.Domain.DataProducts;

namespace Enset.Application.DataProducts.Generation.Models;

public sealed class DataProductGenerationContext
{
    public required GenerateDataProductCommand Command { get; init; }

    public required DataProduct DataProduct { get; init; }

    public required DataProductDefinition Definition { get; init; }

    public DateTime? PeriodFrom => Command.PeriodFrom;

    public DateTime? PeriodTo => Command.PeriodTo;

    public IReadOnlyDictionary<string, string> Parameters =>
        Command.Parameters
        ?? EmptyParameters;

    public DataQuality Quality { get; set; } = DataQuality.Unknown;

    public ICollection<DataProductValue> Values { get; } =
        new List<DataProductValue>();

    public ICollection<string> Warnings { get; } =
        new List<string>();

    private static readonly IReadOnlyDictionary<string, string>
        EmptyParameters = new Dictionary<string, string>();
}