using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.WriteGate;

public sealed class ImportWriteContext
{
    public Guid ImportId { get; init; }

    public ImportReport? Report { get; init; }

    public ImportTargetMode TargetMode { get; init; }

    public ImportWriterType TargetWriter { get; init; }

    public string UserId { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public string? TargetLocation { get; init; }

    public bool ArchiveRawSource { get; init; }

    public IReadOnlyCollection<ImportIssue> Issues =>
        Report?.Issues ?? [];

    public IReadOnlyCollection<CustomerImportDto> Customers =>
        Report?.Customers ?? [];

    public IReadOnlyCollection<BuildingImportDto> Buildings =>
        Report?.Buildings ?? [];

    public IReadOnlyCollection<MeterImportDto> Meters =>
        Report?.Meters ?? [];

    public IReadOnlyCollection<MeterReadingImportDto> MeterReadings =>
        Report?.MeterReadings ?? [];
}