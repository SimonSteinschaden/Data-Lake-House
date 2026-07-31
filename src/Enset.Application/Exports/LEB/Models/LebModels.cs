namespace Enset.Application.Exports.LEB.Models;

using Enset.Application.CanonicalSnapshots;
using Enset.Application.Exports.LEB.Contracts;

public sealed record LebExportRequest(
    Guid? CustomerId = null, DateTime? ReadingFrom = null, DateTime? ReadingTo = null);

public sealed record LebMunicipalityRow(
    Guid MunicipalityId, string? MunicipalityNumber, string MunicipalityName,
    string? MainRegion, DateTime ExportTimestamp);

public sealed record LebObjectRow(
    Guid ObjectId, Guid? MunicipalityId, string? ObjectCategory, string? ObjectCode,
    string ObjectName, string? UsageType, string? UsageClassification,
    string? Street, string? PostalCode, string? City, int? ConstructionYear,
    int? RenovationYear, int? FloorCount, decimal? ConditionedGrossFloorArea,
    decimal? UnconditionedGrossFloorArea, decimal? ConditionedGrossVolume,
    decimal? UnconditionedGrossVolume, string? ReferenceMetricType,
    decimal? ReferenceMetricValue, string? ReferenceMetricUnit,
    string? ContactName, string? ContactPhone, string? ContactEmail);

public sealed record LebMeterRow(
    Guid MeterId, Guid? ObjectId, string MeterName, string MeterNumber,
    string? GridMeteringPointNumber, string? MeterType, string? MeterCategory,
    string? EnergyCarrier, string? NoeNavigatorMedium,
    string? NoeNavigatorMediumGroup, string? MeasurementDirection,
    string? ReadingType, string? Unit, DateTime? ValidFrom, DateTime? ValidTo);

public sealed record LebReadingRow(
    Guid MeterId, DateTime? ReadingTimestamp, decimal? ReadingValue, string? Unit,
    string? ReadingType, string? QualityStatus, string? Source, bool IsCalculated);

public sealed record LebEnergySystemRow(
    Guid EnergySystemId, Guid? ObjectId, string? SupplyPurpose,
    string? EnergyCarrier, decimal? InstalledCapacity, int? ConstructionYear,
    DateTime? ValidFrom, DateTime? ValidTo);

public sealed record LebExportFile(byte[] Content, string ContentType, string FileName);

public sealed record LebExportAssessment(
    string EntityType,
    Guid EntityId,
    string FachlicheIdentifikation,
    string QualityLevel,
    SuitabilityStatus LebSuitability);

public sealed record LebExportDataset(
    NoeLebExportContractV1 Contract,
    IReadOnlyList<LebExportAssessment> Assessments,
    DateTime SnapshotCreatedAt);
