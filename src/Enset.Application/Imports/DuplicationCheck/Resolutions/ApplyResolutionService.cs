using System.Diagnostics;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Resolution;

public sealed class ApplyResolutionService : IApplyResolutionService
{
    public ApplyResolutionRuleResult ApplyRule(
        ImportReport report,
        ApplyResolutionRuleCommand command,
        string userId,
        DateTime timestamp)
    {
        var stopwatch = Stopwatch.StartNew();
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A user id is required.", nameof(userId));
        if (command.Scope == ResolutionScope.FutureImports)
            throw new InvalidOperationException(
                "Resolution rules for future imports are not enabled.");
        if (command.ResolutionAction == ImportResolutionAction.None)
            throw new ArgumentException("A resolution action is required.", nameof(command));
        if (command.ResolutionAction == ImportResolutionAction.UseCustomValue &&
            string.IsNullOrWhiteSpace(command.ResolutionPayload))
            throw new ArgumentException(
                "A resolution payload is required for a custom value.", nameof(command));

        var existingRule = report.ResolutionRules.FirstOrDefault(
            rule => rule.Id == command.RuleId);
        if (existingRule is not null)
        {
            return ToResult(report, existingRule, resolvedIssueCount: 0);
        }

        var seed = report.Issues.FirstOrDefault(issue =>
            issue.IssueId == command.SeedIssueId)
            ?? throw new ArgumentException(
                $"Issue '{command.SeedIssueId}' does not belong to import '{report.ImportId}'.",
                nameof(command));
        ImportResolutionOptionsProvider.Validate(
            seed, command.ResolutionAction, command.ResolutionPayload);
        var effectivePayload = EffectivePayload(
            command.ResolutionAction, command.ResolutionPayload);
        var valuePattern = seed.ValuePattern == ImportIssueValuePattern.None
            ? ImportIssueValuePattern.ExactValue
            : seed.ValuePattern;
        var numberGroup = IsDecimalNumberGroup(seed);
        var matchValue = !numberGroup &&
                         valuePattern == ImportIssueValuePattern.ExactValue
            ? Normalize(seed.FirstValue)
            : null;
        // MapReference carries one concrete target id. Keep the established
        // RDW target isolation even though workbook missing-id representatives
        // are otherwise grouped solely by issue type.
        var compatibilitySourceType =
            command.ResolutionAction == ImportResolutionAction.MapReference
                ? ImportSourceType.Excel
                : report.SourceType;

        IReadOnlyList<ImportIssue> matchingIssues = command.Scope switch
        {
            ResolutionScope.SingleIssue => [seed],
            ResolutionScope.MatchingIssuesInCurrentImport =>
                report.Issues
                    .Where(issue =>
                        ImportIssueCompatibility.MatchesCurrentGroup(
                            issue, seed, compatibilitySourceType))
                    .ToList(),
            ResolutionScope.MatchingIssueTypeInCurrentImport =>
                report.Issues
                    .Where(issue =>
                        ImportIssueCompatibility.MatchesIssueType(
                            issue, seed, compatibilitySourceType))
                    .ToList(),
            _ => throw new InvalidOperationException(
                $"Resolution scope '{command.Scope}' is not enabled.")
        };
        var compatibilityKey = command.Scope ==
                               ResolutionScope.MatchingIssuesInCurrentImport
            ? ImportIssueCompatibility.CurrentGroupKey(
                seed, compatibilitySourceType)
            : ImportIssueCompatibility.IssueTypeCompatibilityKey(
                seed, compatibilitySourceType);
        if (command.Scope is
            ResolutionScope.MatchingIssuesInCurrentImport or
            ResolutionScope.MatchingIssueTypeInCurrentImport)
        {
            var isLebAggregate =
                report.SourceType == ImportSourceType.Landesenergiebuchhaltung &&
                seed.Type is ImportIssueType.InvalidNumberFormat or
                    ImportIssueType.MissingData;
            if (!isLebAggregate &&
                !ImportResolutionOptionsProvider.HaveIdenticalOptions(matchingIssues))
                throw new InvalidOperationException(
                    "A group rule requires identical resolution options for all matching issues.");
            var supportsEveryIssue = matchingIssues.All(issue =>
                ImportResolutionOptionsProvider.GetOptions(issue).Any(option =>
                    option.Action == command.ResolutionAction &&
                    option.SupportsBatch));
            if (!supportsEveryIssue)
                throw new InvalidOperationException(
                    $"Resolution '{command.ResolutionAction}' is not batch-compatible " +
                    "with every issue in the selected group.");
        }

        var rule = new ImportResolutionRule
        {
            Id = command.RuleId,
            ImportId = report.ImportId,
            SourceType = report.SourceType,
            IssueCode = seed.Type,
            FieldName =
                numberGroup &&
                command.Scope ==
                    ResolutionScope.MatchingIssueTypeInCurrentImport
                    ? null
                    : seed.FieldName,
            ValuePattern = valuePattern,
            TargetDataType = seed.TargetDataType,
            NumberFormatPattern = seed.NumberFormatPattern,
            MatchValue = matchValue,
            ResolutionType = command.ResolutionType,
            ResolutionAction = command.ResolutionAction,
            ResolutionPayload = effectivePayload,
            Scope = command.Scope,
            CreatedBy = userId,
            CreatedAt = timestamp,
            AppliedBy = userId,
            AppliedAt = timestamp,
            MatchedIssueCount = matchingIssues.Count
        };
        rule.SkippedIssueCount =
            command.Scope == ResolutionScope.MatchingIssueTypeInCurrentImport
                ? report.Issues.Count(issue =>
                    !issue.IsResolved &&
                    issue.Type == seed.Type &&
                    !ImportIssueCompatibility.MatchesIssueType(
                        issue, seed, compatibilitySourceType))
                : 0;

        var resolvedCount = 0;
        var failedCount = 0;
        foreach (var issue in matchingIssues)
        {
            if (issue.IsResolved)
                continue;
            try
            {
                var issuePayload = effectivePayload;
                if (command.ResolutionAction is ImportResolutionAction.ParseDeAt or
                    ImportResolutionAction.ParseInvariant)
                {
                    var requiredPattern = command.ResolutionAction ==
                                          ImportResolutionAction.ParseDeAt
                        ? NumberFormatPattern.AustrianDecimal
                        : NumberFormatPattern.InvariantDecimal;
                    if (!NumberFormatPatternDetector.TryParse(
                            issue.FirstValue, requiredPattern, out var parsed))
                    {
                        failedCount++;
                        continue;
                    }
                    issuePayload = parsed.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                ApplyReferenceResolution(
                    report,
                    issue,
                    command.ResolutionAction,
                    issuePayload);
                ApplyCsvMappingResolution(
                    report,
                    issue,
                    command.ResolutionAction,
                    issuePayload);
                issue.ResolveByRule(
                    command.ResolutionAction,
                    issuePayload,
                    userId,
                    timestamp,
                    command.Scope,
                    rule.Id);
                resolvedCount++;
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException)
            {
                failedCount++;
                Trace.TraceWarning(
                    $"Resolution issue failed. ImportId={report.ImportId}; " +
                    $"IssueId={issue.IssueId}; IssueType={issue.Type}; " +
                    $"RuleId={rule.Id}; ExceptionType={exception.GetType().Name}");
            }
        }

        rule.ResolvedIssueCount = resolvedCount;
        rule.FailedIssueCount = failedCount;
        report.ResolutionRules.Add(rule);
        report.RecalculateCommitReadiness();
        report.UpdatedAt = timestamp;
        report.AuditTrail.Add(new ImportAuditEntry
        {
            Timestamp = timestamp,
            UserId = userId,
            Action = "ResolutionRuleApplied",
            ResolutionAction = command.ResolutionAction,
            CustomResolvedValue = effectivePayload,
            Details =
                $"RuleId={rule.Id}; Scope={rule.Scope}; IssueCode={rule.IssueCode}; " +
                $"Field={rule.FieldName}; Pattern={rule.ValuePattern}; " +
                $"Matched={rule.MatchedIssueCount}; Resolved={resolvedCount}; " +
                $"Failed={failedCount}"
        });
        stopwatch.Stop();
        Trace.TraceInformation(
            $"Import resolution rule completed. ImportId={report.ImportId}; " +
            $"SeedIssueId={seed.IssueId}; IssueType={seed.Type}; " +
            $"CompatibilityKey={compatibilityKey}; Scope={command.Scope}; " +
            $"MatchedCount={matchingIssues.Count}; ResolvedCount={resolvedCount}; " +
            $"FailedCount={failedCount}; DurationMs={stopwatch.ElapsedMilliseconds}; " +
            $"RemainingBlockingIssueCount={report.BlockingOpenIssueCount}");

        return ToResult(report, rule, resolvedCount);
    }

    public ImportReport Apply(
        ImportReport report,
        IReadOnlyCollection<ImportIssueResolution> resolutions,
        string userId,
        DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(resolutions);

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A user id is required.", nameof(userId));

        EnsureUniqueResolutions(resolutions);

        if (report.Status is ImportStatus.Committing or ImportStatus.Committed)
        {
            throw new InvalidOperationException(
                $"Import '{report.ImportId}' can no longer be changed in status '{report.Status}'.");
        }

        var issuesById = report.Issues.ToDictionary(issue => issue.IssueId);

        foreach (var resolution in resolutions)
        {
            if (!issuesById.TryGetValue(resolution.IssueId, out var issue))
            {
                throw new ArgumentException(
                    $"Issue '{resolution.IssueId}' does not belong to import '{report.ImportId}'.",
                    nameof(resolutions));
            }

            ApplyResolution(report, issue, resolution, userId, timestamp);
        }

        report.RecalculateCommitReadiness();
        report.UpdatedAt = timestamp;

        return report;
    }

    private static void EnsureUniqueResolutions(
        IReadOnlyCollection<ImportIssueResolution> resolutions)
    {
        var duplicateIssueId = resolutions
            .GroupBy(resolution => resolution.IssueId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateIssueId is not null)
        {
            throw new ArgumentException(
                $"More than one resolution was supplied for issue '{duplicateIssueId}'.",
                nameof(resolutions));
        }
    }

    private static void ApplyCsvMappingResolution(
        ImportReport report,
        ImportIssue issue,
        ImportResolutionAction action,
        string? payload)
    {
        if (report.SourceType != ImportSourceType.Csv ||
            report.CsvMapping is null)
            return;

        var mapping = report.CsvMapping;
        switch (action)
        {
            case ImportResolutionAction.SelectTimestampColumn:
                mapping.TimestampColumn = ValidateHeader(mapping, payload);
                mapping.TimestampSource = ImportFieldSource.UserSelectedColumn;
                mapping.StartTimestamp = null;
                mapping.SamplingInterval = null;
                break;
            case ImportResolutionAction.SelectValueColumn:
                mapping.ValueColumn = ValidateHeader(mapping, payload);
                mapping.ValueSource = ImportFieldSource.UserSelectedColumn;
                break;
            case ImportResolutionAction.SelectQualityColumn:
                mapping.QualityColumn = ValidateHeader(mapping, payload);
                mapping.QualitySource = ImportFieldSource.UserSelectedColumn;
                break;
            case ImportResolutionAction.GenerateTimestamps:
                var generation = System.Text.Json.JsonSerializer.Deserialize<
                    TimestampGenerationPayload>(
                    payload ?? string.Empty,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? throw new ArgumentException(
                        "StartTimestamp and SamplingInterval are required.");
                if (generation.SamplingInterval <= TimeSpan.Zero)
                    throw new ArgumentException(
                        "SamplingInterval must be greater than zero.");
                mapping.TimestampColumn = null;
                mapping.TimestampSource = ImportFieldSource.Generated;
                mapping.StartTimestamp =
                    generation.StartTimestamp.ToUniversalTime();
                mapping.SamplingInterval = generation.SamplingInterval;
                break;
            case ImportResolutionAction.AssignMeter:
                if (!Guid.TryParse(payload, out var meterId) ||
                    meterId == Guid.Empty)
                    throw new ArgumentException(
                        "A valid MeterId is required.");
                report.AssignedMeterId = meterId;
                break;
            case ImportResolutionAction.CreateMeter:
                var meterNumber = payload?.Trim();
                if (string.IsNullOrWhiteSpace(meterNumber))
                    throw new ArgumentException(
                        "A meter number is required.");
                report.AssignedMeterId = null;
                report.DefaultMeterNumber = meterNumber;
                if (!report.Meters.Any(meter =>
                        string.Equals(
                            meter.MeterNumber,
                            meterNumber,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    report.Meters = report.Meters
                        .Append(new MeterImportDto
                        {
                            MeterNumber = meterNumber,
                            ProfileName = meterNumber,
                            AllowUnassignedBuilding = true
                        })
                        .ToList();
                }
                break;
            default:
                return;
        }

        report.MeterReadings = CsvMeterReadingMappingService
            .Map(
                mapping,
                report.DefaultMeterNumber,
                report.AssignedMeterId)
            .Select(MeterReadingExcelRowMapper.ToDto)
            .ToList();
    }

    private static string ValidateHeader(
        CsvMeterReadingMapping mapping,
        string? payload)
    {
        var header = mapping.DetectedHeaders.FirstOrDefault(candidate =>
            string.Equals(candidate, payload?.Trim(),
                StringComparison.OrdinalIgnoreCase));
        return header ?? throw new ArgumentException(
            $"CSV header '{payload}' does not exist.");
    }

    private sealed record TimestampGenerationPayload(
        DateTime StartTimestamp,
        TimeSpan SamplingInterval);

    private static ApplyResolutionRuleResult ToResult(
        ImportReport report,
        ImportResolutionRule rule,
        int resolvedIssueCount) => new()
    {
        RuleId = rule.Id,
        MatchedIssueCount = rule.MatchedIssueCount,
        ResolvedIssueCount = resolvedIssueCount,
        FailedIssueCount = rule.FailedIssueCount,
        SkippedIssueCount = rule.SkippedIssueCount,
        RemainingBlockingIssueCount = report.UnresolvedIssueCount,
        Status = report.Status
    };

    private static ImportIssueValuePattern EffectivePattern(ImportIssue issue) =>
        issue.ValuePattern == ImportIssueValuePattern.None
            ? ImportIssueValuePattern.ExactValue
            : issue.ValuePattern;

    private static bool IsDecimalNumberGroup(ImportIssue issue) =>
        issue.Type == ImportIssueType.InvalidNumberFormat &&
        issue.TargetDataType == ResolutionTargetDataType.Decimal &&
        issue.NumberFormatPattern != NumberFormatPattern.None;

    private static bool Matches(
        ImportIssue issue,
        ImportIssue seed,
        ImportIssueValuePattern valuePattern,
        string? matchValue,
        bool numberGroup)
    {
        if (issue.Type != seed.Type)
            return false;
        if (numberGroup)
        {
            return issue.TargetDataType == seed.TargetDataType &&
                   issue.NumberFormatPattern == seed.NumberFormatPattern;
        }

        return string.Equals(
                   issue.FieldName, seed.FieldName,
                   StringComparison.OrdinalIgnoreCase) &&
               EffectivePattern(issue) == valuePattern &&
               (valuePattern != ImportIssueValuePattern.ExactValue ||
                Normalize(issue.FirstValue) == matchValue);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? EffectivePayload(
        ImportResolutionAction action,
        string? suppliedPayload) => action switch
    {
        ImportResolutionAction.SetZero => "0",
        ImportResolutionAction.ParseDeAt => "de-AT",
        ImportResolutionAction.ParseInvariant =>
            System.Globalization.CultureInfo.InvariantCulture.Name,
        _ => string.IsNullOrWhiteSpace(suppliedPayload)
            ? null
            : suppliedPayload.Trim()
    };

    private static void ApplyResolution(
        ImportReport report,
        ImportIssue issue,
        ImportIssueResolution resolution,
        string userId,
        DateTime timestamp)
    {
        if (!issue.RequiresUserDecision &&
            !issue.IsCommitBlocking &&
            ImportResolutionOptionsProvider.GetOptions(issue).Count == 0)
        {
            throw new InvalidOperationException(
                $"Issue '{issue.IssueId}' does not accept a user resolution.");
        }

        if (resolution.ResolutionAction == ImportResolutionAction.UseCustomValue &&
            string.IsNullOrWhiteSpace(resolution.CustomResolvedValue))
        {
            throw new ArgumentException(
                $"A custom value is required for issue '{issue.IssueId}'.",
                nameof(resolution));
        }

        if (resolution.ResolutionAction != ImportResolutionAction.None)
        {
            ImportResolutionOptionsProvider.Validate(
                issue,
                resolution.ResolutionAction,
                resolution.CustomResolvedValue);
        }

        var previousAction = issue.ResolutionAction;
        var previousCustomValue = issue.CustomResolvedValue;
        var effectivePayload = EffectivePayload(
            resolution.ResolutionAction,
            resolution.CustomResolvedValue);
        ApplyReferenceResolution(
            report,
            issue,
            resolution.ResolutionAction,
            effectivePayload);
        ApplyCsvMappingResolution(
            report,
            issue,
            resolution.ResolutionAction,
            effectivePayload);
        if (resolution.ResolutionAction is ImportResolutionAction.ParseDeAt or
            ImportResolutionAction.ParseInvariant)
        {
            var pattern = resolution.ResolutionAction == ImportResolutionAction.ParseDeAt
                ? NumberFormatPattern.AustrianDecimal
                : NumberFormatPattern.InvariantDecimal;
            if (!NumberFormatPatternDetector.TryParse(
                    issue.FirstValue, pattern, out var parsed))
                throw new ArgumentException(
                    $"Value '{issue.FirstValue}' cannot be parsed as '{pattern}'.",
                    nameof(resolution));
            effectivePayload = parsed.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        issue.ResolveManually(
            resolution.ResolutionAction,
            effectivePayload,
            userId,
            timestamp);

        if (issue.Type == ImportIssueType.SourceColumnMappingRequired &&
            resolution.ResolutionAction is ImportResolutionAction.RenameColumn or
                ImportResolutionAction.MapField or
                ImportResolutionAction.UseCustomValue)
        {
            var column = report.SourceColumns.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.EffectiveHeader,
                    issue.FieldName,
                    StringComparison.OrdinalIgnoreCase));
            if (column is not null)
                column.EffectiveHeader = effectivePayload!;
        }

        report.AuditTrail.Add(new ImportAuditEntry
        {
            Timestamp = timestamp,
            UserId = userId,
            Action = issue.IsResolved ? "IssueResolutionChanged" : "IssueResolutionCleared",
            IssueId = issue.IssueId,
            PreviousResolutionAction = previousAction,
            ResolutionAction = issue.ResolutionAction,
            PreviousCustomResolvedValue = previousCustomValue,
            CustomResolvedValue = issue.CustomResolvedValue
        });
    }

    private static void ApplyReferenceResolution(
        ImportReport report,
        ImportIssue issue,
        ImportResolutionAction action,
        string? payload)
    {
        if (issue.Type is not (ImportIssueType.MissingCustomer or
            ImportIssueType.MissingBuilding or
            ImportIssueType.MissingMeter))
            return;

        if (action == ImportResolutionAction.MapReference)
        {
            ApplyReferenceMapping(report, issue, payload);
            return;
        }

        if (action == ImportResolutionAction.CreateNew)
        {
            ApplyCreateNew(report, issue, payload);
            return;
        }

        if (action == ImportResolutionAction.SkipRow)
            ApplySkipRow(report, issue);
    }

    private static void ApplyReferenceMapping(
        ImportReport report,
        ImportIssue issue,
        string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException(
                "MapReference requires a concrete external reference id.",
                nameof(payload));

        if (issue.FieldName == "Building.InternalCustomerId")
        {
            if (!report.Customers.Any(customer =>
                    string.Equals(
                        customer.ExternalCustomerId,
                        payload.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException(
                    $"Customer reference '{payload}' does not exist in this import.",
                    nameof(payload));

            var building = FindBuilding(report, issue.SourceRowNumber);
            if (building is not null)
                building.ExternalCustomerId = payload.Trim();
            return;
        }

        return;
    }

    private static void ApplyCreateNew(
        ImportReport report,
        ImportIssue issue,
        string? payload)
    {
        if (issue.FieldName == "Customer.InternalCustomerId")
        {
            var customer = FindCustomer(report, issue.SourceRowNumber);
            if (customer is not null)
                customer.ExternalCustomerId =
                    NewExternalId("RDW-CUST", issue, payload);
            return;
        }

        if (issue.FieldName == "Building.InternalBuildingId")
        {
            var building = FindBuilding(report, issue.SourceRowNumber);
            if (building is not null)
                building.ExternalBuildingId =
                    NewExternalId("RDW-BLD", issue, payload);
            return;
        }

        if (issue.FieldName == "Building.InternalCustomerId")
        {
            var building = FindBuilding(report, issue.SourceRowNumber);
            if (building is null)
                return;

            var customerId = NewExternalId("RDW-CUST", issue, payload);
            if (!report.Customers.Any(customer =>
                    string.Equals(
                        customer.ExternalCustomerId,
                        customerId,
                        StringComparison.OrdinalIgnoreCase)))
            {
                report.Customers = report.Customers
                    .Append(new CustomerImportDto
                    {
                        ExternalCustomerId = customerId,
                        CompanyName = CustomerName(issue.FirstValue),
                        Street = building.Street,
                        HouseNumber = building.HouseNumber,
                        PostalCode = building.PostalCode,
                        City = building.City,
                        Country = building.Country
                    })
                    .ToList();
            }

            building.ExternalCustomerId = customerId;
            return;
        }

        return;
    }

    private static void ApplySkipRow(
        ImportReport report,
        ImportIssue issue)
    {
        if (issue.FieldName?.StartsWith(
                "Building.",
                StringComparison.Ordinal) == true)
        {
            var building = FindBuilding(report, issue.SourceRowNumber);
            if (building is null)
                return;

            var buildingId = building.ExternalBuildingId;
            var skippedMeterNumbers = report.Meters
                .Where(meter => string.Equals(
                    meter.ExternalBuildingId,
                    buildingId,
                    StringComparison.OrdinalIgnoreCase))
                .Select(meter => meter.MeterNumber)
                .Where(number => !string.IsNullOrWhiteSpace(number))
                .Select(number => number!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            report.Buildings = report.Buildings
                .Where(candidate => candidate.SourceRowNumber !=
                    issue.SourceRowNumber)
                .ToList();
            report.Meters = report.Meters
                .Where(meter => !string.Equals(
                    meter.ExternalBuildingId,
                    buildingId,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
            report.MeterReadings = report.MeterReadings
                .Where(reading =>
                    string.IsNullOrWhiteSpace(reading.MeterNumber) ||
                    !skippedMeterNumbers.Contains(reading.MeterNumber))
                .ToList();
            return;
        }

        if (issue.FieldName == "Customer.InternalCustomerId")
        {
            report.Customers = report.Customers
                .Where(customer => customer.SourceRowNumber !=
                    issue.SourceRowNumber)
                .ToList();
        }
    }

    private static CustomerImportDto? FindCustomer(
        ImportReport report,
        int? sourceRowNumber) =>
        report.Customers.FirstOrDefault(customer =>
            customer.SourceRowNumber == sourceRowNumber);

    private static BuildingImportDto? FindBuilding(
        ImportReport report,
        int? sourceRowNumber) =>
        report.Buildings.FirstOrDefault(building =>
            building.SourceRowNumber == sourceRowNumber);

    private static string NewExternalId(
        string prefix,
        ImportIssue issue,
        string? suppliedValue)
    {
        if (!string.IsNullOrWhiteSpace(suppliedValue))
            return suppliedValue.Trim();

        var seed = string.IsNullOrWhiteSpace(issue.FirstValue)
            ? issue.SourceRowNumber?.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            : issue.FirstValue.Trim().ToUpperInvariant();
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                $"{prefix}|{seed ?? issue.IssueId.ToString("N")}"));
        return $"{prefix}-{Convert.ToHexString(hash)[..12]}";
    }

    private static string CustomerName(string? groupKey)
    {
        var name = groupKey?.Split('|')[0].Trim();
        return string.IsNullOrWhiteSpace(name)
            ? "RDW customer created by import resolution"
            : name;
    }


}
