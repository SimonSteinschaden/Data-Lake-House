using System.Globalization;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.Leb;

public sealed class LebImportValidator(LebWorkbookDto source) : IImportValidator
{
    public ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> meterReadings,
        ImportSourceType sourceType = ImportSourceType.Landesenergiebuchhaltung)
    {
        var report = new ImportReport
        {
            CustomerCount = customers.Count,
            BuildingCount = buildings.Count,
            MeterCount = meters.Count,
            MeterReadingCount = meterReadings.Count,
            SourceColumns = source.Columns
        };

        foreach (var column in source.Columns.Where(column =>
                     column.WasHeaderGenerated && column.HasData))
        {
            report.Issues.Add(new ImportIssue
            {
                Type = ImportIssueType.SourceColumnMappingRequired,
                Severity = ImportIssueSeverity.Warning,
                FieldName = column.EffectiveHeader,
                FirstValue = "(leer)",
                SecondValue = $"Spalte {column.Index}",
                Message =
                    $"Spalte {column.Index} besitzt keine Bezeichnung und wurde " +
                    $"vorläufig als „{column.EffectiveHeader}“ benannt. Sie enthält Daten.",
                RequiresUserDecision = true,
                ValuePattern = ImportIssueValuePattern.EmptyGeneratedHeader
            });
        }

        foreach (var row in source.Rows)
        {
            Required(row.MunicipalityId, "GemID", row.RowNumber, report);
            Required(row.BuildingId, "GebID", row.RowNumber, report);
            Required(row.MeterId, "ZId", row.RowNumber, report);
            Required(row.MeterName, "Zähler", row.RowNumber, report);
            MissingData(row.ConstructionYear, "Baujahr", row.RowNumber, report);
            MissingData(row.FloorArea, "m2", row.RowNumber, report);
            if (string.IsNullOrWhiteSpace(row.AnnualValue))
            {
                AddMissingData(
                    report,
                    row.RowNumber,
                    "AnnualTotal",
                    "Pflichtfeld 'AnnualTotal' fehlt.",
                    valuePattern: row.MonthlyValues.Any(value =>
                        !string.IsNullOrWhiteSpace(value))
                        ? ImportIssueValuePattern.MissingAnnualTotalWithMonthlyValues
                        : ImportIssueValuePattern.ExactValue);
            }

            if (!int.TryParse(row.Year, out _))
                Add(report, row.RowNumber, "ReadingYear",
                    $"Ungültiges Jahr '{row.Year}'.", row.Year,
                    issueType: ImportIssueType.InvalidNumberFormat);

            foreach (var (value, month) in row.MonthlyValues.Select((value, index) => (value, index + 1)))
            {
                if (!string.IsNullOrWhiteSpace(value))
                    AddNumberFormatIssueWhenNeeded(
                        report, row.RowNumber, MonthName(month), value);
            }

            if (!string.IsNullOrWhiteSpace(row.AnnualValue))
                AddNumberFormatIssueWhenNeeded(
                    report, row.RowNumber, "AnnualTotal", row.AnnualValue);
        }

        return report;
    }

    private static void Required(
        string? value, string field, int row, ImportReport report)
    {
        if (string.IsNullOrWhiteSpace(value))
            Add(report, row, field, $"Strukturelles Pflichtfeld '{field}' fehlt.",
                issueType: ImportIssueType.StructuralError);
    }

    private static void MissingData(
        string? value, string field, int row, ImportReport report)
    {
        if (string.IsNullOrWhiteSpace(value))
            AddMissingData(
                report, row, field, $"Feld '{field}' fehlt und bleibt leer.");
    }

    private static void AddMissingData(
        ImportReport report,
        int row,
        string field,
        string message,
        ImportIssueValuePattern valuePattern = ImportIssueValuePattern.ExactValue) =>
        report.Issues.Add(new ImportIssue
        {
            Type = ImportIssueType.MissingData,
            Severity = ImportIssueSeverity.Warning,
            FieldName = field,
            SourceRowNumber = row,
            ValuePattern = valuePattern,
            Message = $"LEB-Zeile {row}: {message}",
            RequiresUserDecision = false
        });

    private static void AddNumberFormatIssueWhenNeeded(
        ImportReport report,
        int row,
        string field,
        string value)
    {
        var pattern = NumberFormatPatternDetector.Detect(value);
        if (pattern == NumberFormatPattern.AmbiguousDecimal)
            return;

        report.Issues.Add(new ImportIssue
        {
            Type = ImportIssueType.InvalidNumberFormat,
            Severity = ImportIssueSeverity.Error,
            FieldName = field,
            SourceRowNumber = row,
            FirstValue = value,
            TargetDataType = ResolutionTargetDataType.Decimal,
            NumberFormatPattern = pattern,
            ValuePattern = pattern switch
            {
                NumberFormatPattern.AustrianDecimal =>
                    ImportIssueValuePattern.GermanDecimal,
                _ => ImportIssueValuePattern.ExactValue
            },
            Message =
                $"LEB-Zeile {row}: Zahlenformat '{value}' wurde als " +
                $"'{pattern}' klassifiziert.",
            RequiresUserDecision = false
        });
    }

    private static void Add(
        ImportReport report,
        int row,
        string field,
        string message,
        string? value = null,
        ImportIssueValuePattern valuePattern = ImportIssueValuePattern.None,
        ImportIssueType issueType = ImportIssueType.InvalidValue) =>
        report.Issues.Add(new ImportIssue
        {
            Type = issueType,
            Severity = ImportIssueSeverity.Error,
            FieldName = field,
            SourceRowNumber = row,
            FirstValue = value,
            ValuePattern = valuePattern,
            Message = $"LEB-Zeile {row}: {message}",
            RequiresUserDecision = false
        });

    private static string MonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMM", CultureInfo.GetCultureInfo("de-AT"));
}
