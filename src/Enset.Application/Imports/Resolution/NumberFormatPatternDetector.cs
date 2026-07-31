using System.Globalization;

namespace Enset.Application.Imports.Resolution;

public static class NumberFormatPatternDetector
{
    private static readonly CultureInfo Austrian =
        CultureInfo.GetCultureInfo("de-AT");

    public static NumberFormatPattern Detect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NumberFormatPattern.Invalid;

        var normalized = value.Trim()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);
        if (normalized.Any(character =>
                !char.IsDigit(character) &&
                character is not '+' and not '-' and not ',' and not '.'))
            return NumberFormatPattern.Invalid;

        var commaCount = normalized.Count(character => character == ',');
        var dotCount = normalized.Count(character => character == '.');
        if (commaCount >= 1 && dotCount >= 1)
        {
            return normalized.LastIndexOf(',') > normalized.LastIndexOf('.') &&
                   commaCount == 1 &&
                   TryParse(normalized, Austrian, out _)
                ? NumberFormatPattern.AustrianDecimal
                : normalized.LastIndexOf('.') > normalized.LastIndexOf(',') &&
                  dotCount == 1 &&
                  TryParse(normalized, CultureInfo.InvariantCulture, out _)
                    ? NumberFormatPattern.InvariantDecimal
                    : NumberFormatPattern.Invalid;
        }

        if (commaCount == 1 && dotCount == 0)
            return TryParse(normalized, Austrian, out _)
                ? NumberFormatPattern.AustrianDecimal
                : NumberFormatPattern.Invalid;

        if (dotCount == 1 && commaCount == 0)
            return TryParse(normalized, CultureInfo.InvariantCulture, out _)
                ? NumberFormatPattern.InvariantDecimal
                : NumberFormatPattern.Invalid;

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out _)
            ? NumberFormatPattern.AmbiguousDecimal
            : NumberFormatPattern.Invalid;
    }

    public static bool TryParse(
        string? value,
        NumberFormatPattern pattern,
        out decimal parsed)
    {
        parsed = default;
        var culture = pattern switch
        {
            NumberFormatPattern.AustrianDecimal => Austrian,
            NumberFormatPattern.InvariantDecimal => CultureInfo.InvariantCulture,
            _ => null
        };
        return culture is not null && TryParse(value, culture, out parsed);
    }

    private static bool TryParse(
        string? value,
        CultureInfo culture,
        out decimal parsed)
    {
        var normalized = value?.Trim()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);
        if (culture.Name == Austrian.Name)
        {
            normalized = normalized?
                .Replace(".", string.Empty)
                .Replace(',', '.');
        }
        else
        {
            normalized = normalized?.Replace(",", string.Empty);
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out parsed);
    }
}
