using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Resolution;

namespace Enset.Application.Imports.Validation;

public class ExcelImportValidator : IImportValidator
{
    public ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> meterReadings,
        ImportSourceType sourceType = ImportSourceType.Excel)
    {
        var report = new ImportReport
        {
            CustomerCount = customers.Count,
            BuildingCount = buildings.Count,
            MeterCount = meters.Count,
            MeterReadingCount = meterReadings.Count
        };

        if (sourceType is ImportSourceType.Excel or
            ImportSourceType.CRM_Excel)
        {
            ValidateCustomers(customers, report.Issues);
            ValidateBuildings(buildings, report.Issues);
            ValidateCustomerBuildingRelations(customers, buildings, report.Issues);
            ValidateMeters(customers, buildings, meters, report.Issues);
        }

        ValidateMeterReadings(meters, meterReadings, report.Issues, sourceType);

        return report;
    }

    private static void ValidateMeters(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        ICollection<ImportIssue> issues)
    {
        var customerIds = customers
            .Select(customer => customer.InternalCustomerId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var buildingIds = buildings
            .Select(building => building.InternalBuildingId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var meter in meters)
        {
            if (string.IsNullOrWhiteSpace(meter.MeterNumber))
            {
                AddIssue(issues, ImportIssueType.InvalidMeterNumber,
                    $"Meter row {meter.RowNumber}: MeterNumber is empty.");
            }

            if (!string.IsNullOrWhiteSpace(meter.ExternalCustomerId) &&
                !customerIds.Contains(meter.ExternalCustomerId.Trim()))
            {
                AddIssue(issues, ImportIssueType.MissingCustomer,
                    $"Meter row {meter.RowNumber}: references unknown customer '{meter.ExternalCustomerId}'.");
            }

            if (!string.IsNullOrWhiteSpace(meter.ExternalBuildingId) &&
                !buildingIds.Contains(meter.ExternalBuildingId.Trim()))
            {
                AddIssue(issues, ImportIssueType.MissingBuilding,
                    $"Meter row {meter.RowNumber}: references unknown building '{meter.ExternalBuildingId}'.");
            }
        }

        foreach (var duplicate in meters
            .Where(meter => !string.IsNullOrWhiteSpace(meter.MeterNumber))
            .GroupBy(meter => meter.MeterNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.DuplicateMeter,
                Severity = ImportIssueSeverity.Error,
                Message =
                    $"Duplicate MeterNumber '{duplicate.Key}' found in rows: " +
                    $"{string.Join(", ", duplicate.Select(row => row.RowNumber))}.",
                RequiresUserDecision = true,
                FieldName = "MeterIdentity",
                FirstValue = duplicate.Key,
                ValuePattern = ImportIssueValuePattern.ExactValue
            });
        }
    }

    private static void ValidateMeterReadings(
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> readings,
        ICollection<ImportIssue> issues,
        ImportSourceType sourceType)
    {
        var meterNumbers = meters
            .Select(meter => meter.MeterNumber?.Trim())
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var reading in readings)
        {
            if ((sourceType != ImportSourceType.Csv &&
                 string.IsNullOrWhiteSpace(reading.MeterNumber)) ||
                ((sourceType is ImportSourceType.Excel or
                    ImportSourceType.CRM_Excel) &&
                 !string.IsNullOrWhiteSpace(reading.MeterNumber) &&
                 !meterNumbers.Contains(reading.MeterNumber.Trim())))
            {
                AddIssue(issues, ImportIssueType.MissingMeter,
                    $"MeterReading row {reading.RowNumber}: references unknown MeterNumber '{reading.MeterNumber}'.");
            }

            var mapped = MeterReadingExcelRowMapper.ToDto(reading);
            if (!mapped.HasError)
                continue;

            var issueType = mapped.ErrorMessage?.Contains("Timestamp", StringComparison.Ordinal) == true
                ? ImportIssueType.InvalidTimestamp
                : ImportIssueType.InvalidValue;
            AddIssue(issues, issueType,
                $"MeterReading row {reading.RowNumber}: {mapped.ErrorMessage}.");
        }

        foreach (var duplicate in readings
            .Select(reading => new
            {
                Row = reading,
                Mapped = MeterReadingExcelRowMapper.ToDto(reading)
            })
            .Where(item => !item.Mapped.HasError &&
                           !string.IsNullOrWhiteSpace(item.Mapped.MeterNumber))
            .GroupBy(item => new
            {
                MeterNumber = item.Mapped.MeterNumber.ToUpperInvariant(),
                item.Mapped.Timestamp
            })
            .Where(group => group.Count() > 1))
        {
            var issue = new ImportIssue
            {
                Type = ImportIssueType.InvalidValue,
                Severity = ImportIssueSeverity.Error,
                RequiresUserDecision = false,
                FieldName = "MeterNumber,Timestamp",
                FirstValue = duplicate.Key.MeterNumber,
                Message =
                    $"Duplicate meter reading for MeterNumber '{duplicate.Key.MeterNumber}' " +
                    $"and Timestamp '{duplicate.Key.Timestamp:O}' in rows: " +
                    $"{string.Join(", ", duplicate.Select(item => item.Row.RowNumber))}."
            };
            issue.ResolveAutomatically(
                ImportResolutionAction.KeepFirst,
                DateTime.UtcNow);
            issues.Add(issue);
        }

        var mappedReadings = readings
            .Select(MeterReadingExcelRowMapper.ToDto)
            .Where(reading =>
                !reading.HasError &&
                !string.IsNullOrWhiteSpace(reading.MeterNumber))
            .ToList();
        foreach (var series in mappedReadings.GroupBy(
                     reading => reading.MeterNumber,
                     StringComparer.OrdinalIgnoreCase))
        {
            var timestamps = series
                .Where(reading => reading.Timestamp.HasValue)
                .Select(reading => reading.Timestamp!.Value)
                .Distinct()
                .OrderBy(timestamp => timestamp)
                .ToList();
            var gaps = timestamps
                .Zip(timestamps.Skip(1), (left, right) =>
                    (right - left).TotalSeconds)
                .Where(seconds => seconds > 0)
                .Distinct()
                .ToList();
            if (gaps.Count > 1)
            {
                issues.Add(new ImportIssue
                {
                    Type = ImportIssueType.InvalidValue,
                    Severity = ImportIssueSeverity.Warning,
                    FieldName = "Interval",
                    FirstValue = series.Key,
                    Message =
                        $"Meter '{series.Key}' contains mixed timestamp intervals; no interval is persisted.",
                    RequiresUserDecision = false
                });
            }
        }

        foreach (var unit in mappedReadings
                     .Select(reading => reading.Unit?.Trim())
                     .Where(unit => !string.IsNullOrWhiteSpace(unit))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(unit => !IsKnownPhysicalUnit(unit!)))
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.InvalidValue,
                Severity = ImportIssueSeverity.Warning,
                FieldName = "Unit",
                FirstValue = unit,
                Message =
                    $"Unit '{unit}' cannot be mapped to a physical quantity; Quantity remains Unknown.",
                RequiresUserDecision = false
            });
        }
    }

    private static bool IsKnownPhysicalUnit(string unit) =>
        new[]
        {
            "wh", "kwh", "mwh", "w", "kw", "mw", "m3", "m³",
            "m3/h", "m³/h", "l", "l/s", "°c", "c", "k", "pa",
            "bar", "v", "a", "hz", "w/m2", "w/m²", "m/s", "%"
        }.Contains(
            unit.Replace(" ", string.Empty).ToLowerInvariant(),
            StringComparer.Ordinal);

    private static void AddIssue(
        ICollection<ImportIssue> issues,
        ImportIssueType type,
        string message,
        bool requiresUserDecision = false)
    {
        issues.Add(new ImportIssue
        {
            Type = type,
            Severity = ImportIssueSeverity.Error,
            Message = message,
            RequiresUserDecision = requiresUserDecision
        });
    }

    private static void ValidateCustomers(
        IReadOnlyList<CustomerExcelRow> customers,
        ICollection<ImportIssue> issues)
    {
        if (customers.Count == 0)
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = "No customers found.",
                RequiresUserDecision = false
            });
        }

        foreach (var customer in customers.Where(c => string.IsNullOrWhiteSpace(c.InternalCustomerId)))
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = $"Customer row {customer.RowNumber}: InternalCustomerId is empty.",
                RequiresUserDecision = true,
                FieldName = "Customer.InternalCustomerId",
                SourceRowNumber = customer.RowNumber,
                FirstValue = CustomerGroupKey(customer)
            });
        }

        var duplicateIds = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .GroupBy(c => c.InternalCustomerId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(c => c.RowNumber));

            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.DuplicateCustomer,
                Severity = ImportIssueSeverity.Error,
                Message = $"Duplicate CustomerID '{group.Key}' found in rows: {rows}.",
                RequiresUserDecision = true
            });
        }
    }

    private static void ValidateBuildings(
        IReadOnlyList<BuildingExcelRow> buildings,
        ICollection<ImportIssue> issues)
    {
        if (buildings.Count == 0)
        {
            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingBuilding,
                Severity = ImportIssueSeverity.Error,
                Message = "No buildings found.",
                RequiresUserDecision = false
            });
        }

        foreach (var building in buildings)
        {
            if (string.IsNullOrWhiteSpace(building.InternalBuildingId))
            {
                issues.Add(new ImportIssue
                {
                    Type = ImportIssueType.MissingBuilding,
                    Severity = ImportIssueSeverity.Error,
                    Message = $"Building row {building.RowNumber}: InternalBuildingId is empty.",
                    RequiresUserDecision = true,
                    FieldName = "Building.InternalBuildingId",
                    SourceRowNumber = building.RowNumber,
                    FirstValue = BuildingGroupKey(building)
                });
            }
        }

        var duplicateIds = buildings
            .Where(b => !string.IsNullOrWhiteSpace(b.InternalBuildingId))
            .GroupBy(b => b.InternalBuildingId!.Trim())
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateIds)
        {
            var rows = string.Join(", ", group.Select(b => b.RowNumber));

            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.DuplicateBuilding,
                Severity = ImportIssueSeverity.Error,
                Message = $"Duplicate BuildingID '{group.Key}' found in rows: {rows}.",
                RequiresUserDecision = true
            });
        }
    }

    private static void ValidateCustomerBuildingRelations(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        ICollection<ImportIssue> issues)
    {
        var validCustomers = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
            .ToList();

        foreach (var building in buildings)
        {
            var suggestion = FindUniqueCustomerSuggestion(
                building,
                validCustomers);
            var currentId = Normalize(building.InternalCustomerId);
            var currentExists = validCustomers.Any(customer =>
                Normalize(customer.InternalCustomerId) == currentId);

            if (suggestion is not null &&
                currentId == Normalize(suggestion.Customer.InternalCustomerId))
                continue;

            if (suggestion is null && currentExists)
                continue;

            var suggestedId = suggestion?.Customer.InternalCustomerId?.Trim();
            var description = suggestion is null
                ? "No unique customer reference could be reconstructed."
                : $"Unique suggestion '{suggestedId}' based on {suggestion.Evidence}.";

            issues.Add(new ImportIssue
            {
                Type = ImportIssueType.MissingCustomer,
                Severity = ImportIssueSeverity.Error,
                Message =
                    $"Building row {building.RowNumber}: customer reference " +
                    $"'{building.InternalCustomerId}' is missing or inconsistent. " +
                    description,
                RequiresUserDecision = true,
                FieldName = "Building.InternalCustomerId",
                SourceRowNumber = building.RowNumber,
                FirstValue = BuildingCustomerGroupKey(building),
                SecondValue = suggestedId
            });
        }
    }

    private static CustomerSuggestion? FindUniqueCustomerSuggestion(
        BuildingExcelRow building,
        IReadOnlyList<CustomerExcelRow> customers)
    {
        var candidates = customers
            .Select(customer => ScoreCandidate(building, customer))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var highestScore = candidates.Max(candidate => candidate.Score);
        var best = candidates
            .Where(candidate => candidate.Score == highestScore)
            .GroupBy(candidate => Normalize(candidate.Customer.InternalCustomerId))
            .Select(group => group.First())
            .ToList();

        return best.Count == 1 ? best[0] : null;
    }

    private static CustomerSuggestion? ScoreCandidate(
        BuildingExcelRow building,
        CustomerExcelRow customer)
    {
        var folder = Same(building.FolderNumber, customer.FolderNumber);
        var project = Same(building.ProjectName, customer.ProjectName);
        var organization = Same(
            building.OrganizationName,
            customer.OrganizationName);
        var addressMatches = new[]
            {
                Same(building.PostalCode, customer.PostalCode),
                Same(building.City, customer.City),
                Same(building.Street, customer.Street),
                Same(building.HouseNumber, customer.HouseNumber)
            }
            .Count(matches => matches);

        var hasStrongEvidence =
            folder ||
            (project && organization) ||
            (organization && addressMatches >= 1) ||
            addressMatches >= 3;
        if (!hasStrongEvidence)
            return null;

        var evidence = new List<string>();
        if (folder)
            evidence.Add("FolderNumber");
        if (project && organization)
            evidence.Add("ProjectName+OrganizationName");
        if (organization && addressMatches >= 1)
            evidence.Add($"OrganizationName+Address({addressMatches})");
        if (addressMatches >= 3)
            evidence.Add($"Address({addressMatches})");

        return new CustomerSuggestion(
            customer,
            (folder ? 100 : 0) +
            (project ? 20 : 0) +
            (organization ? 10 : 0) +
            addressMatches,
            string.Join(", ", evidence));
    }

    private static string CustomerGroupKey(CustomerExcelRow customer) =>
        string.Join("|",
            Normalize(customer.OrganizationName),
            Normalize(customer.PostalCode),
            Normalize(customer.City),
            Normalize(customer.Street),
            Normalize(customer.HouseNumber));

    private static string BuildingCustomerGroupKey(BuildingExcelRow building) =>
        string.Join("|",
            Normalize(building.OrganizationName),
            Normalize(building.PostalCode),
            Normalize(building.City),
            Normalize(building.Street),
            Normalize(building.HouseNumber));

    private static string BuildingGroupKey(BuildingExcelRow building) =>
        string.Join("|",
            Normalize(building.ProjectName),
            Normalize(building.OrganizationName),
            Normalize(building.PostalCode),
            Normalize(building.City),
            Normalize(building.Street),
            Normalize(building.HouseNumber));

    private static bool Same(string? first, string? second)
    {
        var normalized = Normalize(first);
        return normalized is not null && normalized == Normalize(second);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();

    private sealed record CustomerSuggestion(
        CustomerExcelRow Customer,
        int Score,
        string Evidence);
}
