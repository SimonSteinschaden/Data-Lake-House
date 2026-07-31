namespace Enset.Application.Exports.LEB.Mapping;

public static class NoeEnergyCarrierMapper
{
    private static readonly IReadOnlyDictionary<string, (string Carrier, string Medium, string Group)>
        Values = new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Electricity"] = ("Strom", "STROM", "ELEKTRISCH"),
            ["Gas"] = ("Erdgas", "ERDGAS", "BRENNSTOFF"),
            ["Heat"] = ("Wärme", "WAERME", "WAERME"),
            ["DistrictHeating"] = ("Fernwärme", "FERNWAERME", "WAERME"),
            ["Water"] = ("Wasser", "WASSER", "WASSER"),
            ["Cooling"] = ("Kälte", "KAELTE", "KAELTE"),
            ["Steam"] = ("Dampf", "DAMPF", "WAERME"),
            ["Hydrogen"] = ("Wasserstoff", "WASSERSTOFF", "BRENNSTOFF")
        };

    public static (string? Carrier, string? Medium, string? Group) Map(string? value) =>
        value is not null && Values.TryGetValue(value, out var mapped)
            ? mapped : (null, null, null);
}

public static class NoeBuildingUsageMapper
{
    private static readonly IReadOnlyDictionary<string, string> Values =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Residential"] = "WOHNEN", ["Commercial"] = "GEWERBE",
            ["Public"] = "OEFFENTLICH", ["Mixed"] = "MISCHNUTZUNG",
            ["Office"] = "BUERO",
            ["School"] = "SCHULE", ["Kindergarten"] = "KINDERGARTEN",
            ["Hospital"] = "GESUNDHEIT", ["SportsFacility"] = "SPORT",
            ["Retail"] = "HANDEL", ["Industrial"] = "INDUSTRIE",
            ["MixedUse"] = "MISCHNUTZUNG", ["Other"] = "SONSTIGE"
        };
    public static string? Map(string? value) => value is not null &&
        Values.TryGetValue(value, out var mapped) ? mapped : null;
}

public static class NoeMeterCategoryMapper
{
    private static readonly IReadOnlyDictionary<string, string> Values =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Physical"] = "HAUPTZAEHLER", ["Virtual"] = "VIRTUELL",
            ["Calculated"] = "BERECHNET", ["Aggregated"] = "SAMMELZAEHLER"
        };
    public static string? Map(string? value) => value is not null &&
        Values.TryGetValue(value, out var mapped) ? mapped : null;
}

public static class NoeMeasurementDirectionMapper
{
    public static string? Map(string? value) => value switch
    {
        "Consumption" => "BEZUG", "Production" => "EINSPEISUNG",
        "Bidirectional" => "BIDIREKTIONAL", _ => null
    };
}

public static class NoeReadingTypeMapper
{
    public static string? Map(string? value) => value switch
    {
        "IntervalValue" => "INTERVALLWERT", "CumulativeValue" => "ZAEHLERSTAND",
        "Instantaneous" => "MOMENTANWERT", "Calculated" => "BERECHNET", _ => null
    };
}
