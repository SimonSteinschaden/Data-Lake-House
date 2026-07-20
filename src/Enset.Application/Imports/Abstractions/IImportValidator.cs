using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;

namespace Enset.Application.Imports.Abstractions;

public interface IImportValidator
{
    ImportReport Validate(
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings,
        IReadOnlyList<MeterExcelRow> meters,
        IReadOnlyList<MeterReadingExcelRow> meterReadings);
}
