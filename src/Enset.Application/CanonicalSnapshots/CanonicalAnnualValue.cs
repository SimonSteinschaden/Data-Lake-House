using Enset.Domain.Energy;

namespace Enset.Application.CanonicalSnapshots;

public static class CanonicalAnnualValue
{
    public static (decimal? Value, AnnualValueStatus Status) Evaluate(
        IReadOnlyCollection<(DateTime Timestamp, decimal Value)> readings,
        MeterReadingType? readingType)
    {
        if (readings.Count == 0)
            return (null, AnnualValueStatus.NotAvailable);

        var ordered = readings.OrderBy(x => x.Timestamp).ToArray();
        var year = ordered[0].Timestamp.Year;
        if (ordered.Any(x => x.Timestamp.Year != year))
            return (null, AnnualValueStatus.IncompleteYear);

        var months = ordered
            .Select(x => x.Timestamp.Month)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        if (months.Length != 12)
            return (null, AnnualValueStatus.IncompleteYear);

        var value = readingType == MeterReadingType.CumulativeValue
            ? ordered[^1].Value - ordered[0].Value
            : ordered.Sum(x => x.Value);
        return (value, AnnualValueStatus.CompleteYear);
    }
}
