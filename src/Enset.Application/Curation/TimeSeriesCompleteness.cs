namespace Enset.Application.Curation;

public sealed record TimeSeriesCompleteness(long Expected, long Actual,
    long Missing, long DuplicateTimestamps, decimal CompletenessPercentage);

public static class TimeSeriesCompletenessCalculator
{
    public static TimeSeriesCompleteness Calculate(
        IEnumerable<DateTime> timestampsUtc, int intervalMinutes)
    {
        if (intervalMinutes <= 0)
            throw new CurationValidationException("Das Messintervall muss größer als 0 sein.");
        var values = timestampsUtc.Select(x => x.Kind == DateTimeKind.Utc
                ? x : x.ToUniversalTime()).OrderBy(x => x).ToList();
        if (values.Count == 0) return new(0, 0, 0, 0, 0);
        var distinct = values.Distinct().ToList();
        var expected = (long)Math.Floor(
            (distinct[^1] - distinct[0]).TotalMinutes / intervalMinutes) + 1;
        var actual = distinct.LongCount();
        return new(expected, actual, Math.Max(0, expected - actual),
            values.LongCount() - actual,
            expected == 0 ? 0 : Math.Round(actual * 100m / expected, 2));
    }
}
