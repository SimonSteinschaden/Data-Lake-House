using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.Validation;

public sealed class MeterReadingValidator
{
    public IReadOnlyList<string> Validate(
        MeterReadingImportDto reading,
        bool timestampMayBeGenerated)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(reading.MeterNumber))
            errors.Add("MeterNumber is missing.");
        if (reading.Timestamp is null && !timestampMayBeGenerated)
            errors.Add("Timestamp is missing.");
        if (!string.IsNullOrWhiteSpace(reading.ErrorMessage))
            errors.Add(reading.ErrorMessage);

        return errors;
    }
}
