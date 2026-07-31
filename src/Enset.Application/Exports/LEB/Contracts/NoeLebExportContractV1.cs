using Enset.Application.Exports.LEB.Models;

namespace Enset.Application.Exports.LEB.Contracts;

public sealed record NoeLebExportContractV1(
    string ContractName,
    string ContractVersion,
    DateTime ExportTimestamp,
    IReadOnlyList<LebMunicipalityRow> Municipalities,
    IReadOnlyList<LebObjectRow> Objects,
    IReadOnlyList<LebMeterRow> Meters,
    IReadOnlyList<LebReadingRow> Readings,
    IReadOnlyList<LebEnergySystemRow> EnergySystems)
{
    public const string Name = "NoeLebExportContractV1";
    public const string Version = "1.0";
}
