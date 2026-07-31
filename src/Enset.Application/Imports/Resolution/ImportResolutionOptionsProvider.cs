using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public static class ImportResolutionOptionsProvider
{
    public static IReadOnlyList<AllowedImportResolution> GetOptions(
        ImportIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        return issue.Type switch
        {
            ImportIssueType.DuplicateCustomer or
            ImportIssueType.DuplicateBuilding or
            ImportIssueType.DuplicateMeter => DuplicateOptions,

            ImportIssueType.MissingData => MissingDataOptions(issue.FieldName),

            ImportIssueType.InvalidNumberFormat => NumberOptions(issue),

            ImportIssueType.InvalidTimestamp or
            ImportIssueType.InvalidDateFormat => DateOptions,

            ImportIssueType.MissingCustomer or
            ImportIssueType.MissingBuilding or
            ImportIssueType.MissingMeter => ReferenceOptions,

            ImportIssueType.SourceColumnMappingRequired => GeneratedHeaderOptions,

            ImportIssueType.InvalidValue => GenericInvalidValueOptions,

            ImportIssueType.InvalidAddress or
            ImportIssueType.InvalidPostalCode or
            ImportIssueType.InvalidMeterNumber => GenericInvalidValueOptions,

            ImportIssueType.StructuralError => [],
            ImportIssueType.TimestampColumnSelectionRequired =>
                TimestampColumnOptions,
            ImportIssueType.ValueColumnSelectionRequired =>
                ValueColumnOptions,
            ImportIssueType.AssignMeterRequired => AssignMeterOptions,
            _ => []
        };
    }

    public static void Validate(
        ImportIssue issue,
        ImportResolutionAction action,
        string? payload)
    {
        var option = GetOptions(issue).FirstOrDefault(x => x.Action == action);
        if (option is null && IsLegacyCompatible(issue.Type, action))
        {
            if (action == ImportResolutionAction.UseCustomValue &&
                string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException(
                    $"Resolution '{action}' requires an input value.", nameof(payload));
            return;
        }
        if (option is null)
            throw new InvalidOperationException(
                $"Resolution '{action}' is not allowed for issue type '{issue.Type}'.");

        if (option.RequiresInput && string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException(
                $"Resolution '{action}' requires an input value.", nameof(payload));

        if (option.RequiresInput)
            ValidateInput(option.InputType, payload!);
    }

    private static bool IsLegacyCompatible(
        ImportIssueType issueType,
        ImportResolutionAction action) =>
        (action == ImportResolutionAction.UseCustomValue &&
         issueType is ImportIssueType.DuplicateCustomer or
             ImportIssueType.DuplicateBuilding or
             ImportIssueType.DuplicateMeter or
             ImportIssueType.SourceColumnMappingRequired) ||
        (action == ImportResolutionAction.KeepFirst &&
         issueType == ImportIssueType.InvalidValue);

    public static bool HaveIdenticalOptions(IEnumerable<ImportIssue> issues)
    {
        string Signature(ImportIssue issue) => string.Join(
            "|",
            GetOptions(issue).Select(option =>
                $"{option.Action}:{option.InputType}:{option.SupportsBatch}"));

        return issues.Select(Signature).Distinct(StringComparer.Ordinal).Take(2).Count() <= 1;
    }

    private static void ValidateInput(ResolutionInputType inputType, string value)
    {
        var valid = inputType switch
        {
            ResolutionInputType.Decimal => TryDecimal(value),
            ResolutionInputType.Integer => int.TryParse(value, out _),
            ResolutionInputType.Date => DateTime.TryParse(value, out _),
            _ => true
        };
        if (!valid)
            throw new ArgumentException(
                $"Value '{value}' is not valid for input type '{inputType}'.",
                nameof(value));
    }

    private static bool TryDecimal(string value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.GetCultureInfo("de-AT"), out _) ||
        decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out _);

    private static IReadOnlyList<AllowedImportResolution> MissingDataOptions(
        string? fieldName)
    {
        var inputType = InputTypeFor(fieldName);
        var options = new List<AllowedImportResolution>
        {
            Option(ImportResolutionAction.EnterValue, "Wert ausfüllen",
                true, inputType, supportsBatch: true),
            Option(ImportResolutionAction.IgnoreMissingValue,
                "Fehlenden Wert bewusst akzeptieren", supportsBatch: true)
        };
        if (inputType is ResolutionInputType.Decimal or ResolutionInputType.Integer)
            options.Add(Option(
                ImportResolutionAction.SetZero, "Explizit 0 setzen", supportsBatch: true));
        return options;
    }

    private static IReadOnlyList<AllowedImportResolution> NumberOptions(
        ImportIssue issue)
    {
        var common = new List<AllowedImportResolution>
        {
            Option(ImportResolutionAction.IgnoreInvalidValue,
                "Ungültigen Wert ignorieren", supportsBatch: true),
            Option(ImportResolutionAction.SetZero,
                "Explizit 0 setzen", supportsBatch: true),
            Option(ImportResolutionAction.SkipRow,
                "Zeile überspringen", supportsBatch: true)
        };
        switch (issue.NumberFormatPattern)
        {
            case NumberFormatPattern.AustrianDecimal:
                common.Insert(0, Option(
                    ImportResolutionAction.ParseDeAt,
                    "Als österreichische Zahl interpretieren",
                    supportsBatch: true,
                    culture: "de-AT"));
                break;
            case NumberFormatPattern.InvariantDecimal:
                common.Insert(0, Option(
                    ImportResolutionAction.ParseInvariant,
                    "Als internationale Zahl interpretieren",
                    supportsBatch: true,
                    culture: System.Globalization.CultureInfo.InvariantCulture.Name));
                break;
            case NumberFormatPattern.AmbiguousDecimal:
                common.Insert(0, Option(
                    ImportResolutionAction.EnterValue,
                    "Wert manuell prüfen",
                    true,
                    InputTypeFor(issue.FieldName)));
                break;
            default:
                common.Insert(0, Option(
                    ImportResolutionAction.EnterValue,
                    "Wert manuell eingeben",
                    true,
                    InputTypeFor(issue.FieldName)));
                break;
        }
        return common;
    }

    private static ResolutionInputType InputTypeFor(string? fieldName) =>
        fieldName?.ToUpperInvariant() switch
        {
            "BAUJAHR" or "CONSTRUCTIONYEAR" or "READINGYEAR" =>
                ResolutionInputType.Integer,
            "ANNUALTOTAL" or "JAHR (JAHRESWERT)" or "M2" or "FLOORAREA" or
            "JAN" or "FEB" or "MRZ" or "APR" or "MAI" or "JUN" or
            "JUL" or "AUG" or "SEP" or "OKT" or "NOV" or "DEZ" =>
                ResolutionInputType.Decimal,
            _ => ResolutionInputType.Text
        };

    private static AllowedImportResolution Option(
        ImportResolutionAction action,
        string label,
        bool requiresInput = false,
        ResolutionInputType inputType = ResolutionInputType.None,
        bool supportsBatch = false,
        string? culture = null) => new()
    {
        Action = action,
        Label = label,
        RequiresInput = requiresInput,
        InputType = inputType,
        SupportsBatch = supportsBatch,
        Culture = culture
    };

    private static readonly IReadOnlyList<AllowedImportResolution> DuplicateOptions =
    [
        Option(ImportResolutionAction.KeepFirst, "Ersten Wert behalten",
            supportsBatch: true),
        Option(ImportResolutionAction.KeepSecond, "Zweiten Wert übernehmen",
            supportsBatch: true),
        Option(ImportResolutionAction.Merge, "Datensätze zusammenführen",
            supportsBatch: true),
        Option(ImportResolutionAction.KeepSeparate, "Beide Datensätze behalten",
            supportsBatch: true),
        Option(ImportResolutionAction.SkipRow, "Datensatz überspringen",
            supportsBatch: true)
    ];

    private static readonly IReadOnlyList<AllowedImportResolution> DateOptions =
    [
        Option(ImportResolutionAction.ParseKnownDateFormat,
            "Bekanntes Datumsformat anwenden", supportsBatch: true),
        Option(ImportResolutionAction.EnterValue, "Datum manuell eingeben",
            true, ResolutionInputType.Date),
        Option(ImportResolutionAction.IgnoreInvalidValue, "Wert ignorieren"),
        Option(ImportResolutionAction.SkipRow, "Zeile überspringen")
    ];

    private static readonly IReadOnlyList<AllowedImportResolution> ReferenceOptions =
    [
        Option(ImportResolutionAction.MapReference, "Referenz zuordnen",
            true, ResolutionInputType.Reference, supportsBatch: true),
        Option(ImportResolutionAction.CreateNew, "Neu anlegen",
            supportsBatch: true),
        Option(ImportResolutionAction.SkipRow, "Zeile überspringen",
            supportsBatch: true)
    ];

    private static readonly IReadOnlyList<AllowedImportResolution> GeneratedHeaderOptions =
    [
        Option(ImportResolutionAction.ConfirmGeneratedHeader,
            "Generierten Namen bestätigen", supportsBatch: true),
        Option(ImportResolutionAction.RenameColumn, "Spalte umbenennen",
            true, ResolutionInputType.Text, supportsBatch: true),
        Option(ImportResolutionAction.MapField, "Bekanntem Feld zuordnen",
            true, ResolutionInputType.Text, supportsBatch: true),
        Option(ImportResolutionAction.IgnoreColumn, "Spalte als Zusatzspalte übernehmen",
            supportsBatch: true)
    ];

    private static readonly IReadOnlyList<AllowedImportResolution>
        GenericInvalidValueOptions =
    [
        Option(ImportResolutionAction.EnterValue, "Wert manuell eingeben",
            true, ResolutionInputType.Text),
        Option(ImportResolutionAction.IgnoreInvalidValue, "Wert ignorieren"),
        Option(ImportResolutionAction.SkipRow, "Zeile überspringen")
    ];

    private static readonly IReadOnlyList<AllowedImportResolution>
        TimestampColumnOptions =
    [
        Option(ImportResolutionAction.SelectTimestampColumn,
            "Timestamp-Spalte auswählen", true, ResolutionInputType.Text),
        Option(ImportResolutionAction.GenerateTimestamps,
            "Timestamps aus Startzeit und Intervall erzeugen",
            true, ResolutionInputType.Text)
    ];

    private static readonly IReadOnlyList<AllowedImportResolution>
        ValueColumnOptions =
    [
        Option(ImportResolutionAction.SelectValueColumn,
            "Wertespalte auswählen", true, ResolutionInputType.Text)
    ];

    private static readonly IReadOnlyList<AllowedImportResolution>
        AssignMeterOptions =
    [
        Option(ImportResolutionAction.AssignMeter,
            "Vorhandenen Zähler auswählen", true,
            ResolutionInputType.Reference),
        Option(ImportResolutionAction.CreateMeter,
            "Neuen Zähler anlegen", true,
            ResolutionInputType.Text)
    ];
}
