namespace Enset.Domain.DataProducts;

/// <summary>
/// Kennzeichnet die fachliche Art des Ergebnisses eines Datenprodukts.
/// </summary>
public enum DataProductResultType
{
    Actual = 1,
    Aggregated = 2,
    Calculated = 3,
    Forecast = 4,
    Scenario = 5,
    Recommendation = 6
}
