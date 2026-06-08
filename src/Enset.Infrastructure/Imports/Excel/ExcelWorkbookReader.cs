using ClosedXML.Excel;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Excel;

public class ExcelWorkbookReader : IExcelReader
{
    public IReadOnlyList<CustomerExcelRow> ReadCustomers(string filePath)
    {
        using var workbook = OpenWorkbookSnapshot(filePath);

        var worksheet = workbook.Worksheet("Customers");
        var table = worksheet.Table("Customer");

        var rows = new List<CustomerExcelRow>();

        foreach (var row in table.DataRange.Rows())
        {
            rows.Add(new CustomerExcelRow
            {
                RowNumber = row.RowNumber(),

                InternalCustomerId = GetCellValue(row, table, "InternalCustomerId"),
                FolderNumber = GetCellValue(row, table, "FolderNumber"),
                EKutCustomer = GetCellValue(row, table, "eKUTCustomer"),
                ProjectName = GetCellValue(row, table, "ProjectName"),
                OrganizationName = GetCellValue(row, table, "OrganizationName"),
                FirstName = GetCellValue(row, table, "FirstName"),
                LastName = GetCellValue(row, table, "LastName"),
                PhoneNumber = GetCellValue(row, table, "PhoneNumber"),
                Email = GetCellValue(row, table, "Email"),
                PostalCode = GetCellValue(row, table, "PostalCode"),
                City = GetCellValue(row, table, "City"),
                Street = GetCellValue(row, table, "Street"),
                HouseNumber = GetCellValue(row, table, "HouseNumber"),
                AddressAddtion = GetCellValue(row, table, "AddressAddtion"),
                Prosumer = GetCellValue(row, table, "Prosumer"),
                HasLoadProfile = GetCellValue(row, table, "HasLoadProfile"),
                BuildingRenovationInterest = GetCellValue(row, table, "BuildingRenovationInterest"),
                EnergyCommunityInterest = GetCellValue(row, table, "EnergyCommunityInterest"),
                MobilityInterest = GetCellValue(row, table, "MobilityInterest"),
                PlusEnergyInterest = GetCellValue(row, table, "PlusEnergyInterest"),
                Subsidies = GetCellValue(row, table, "Subsidies"),
                LastContact = GetCellValue(row, table, "LastContact"),
                Notes = GetCellValue(row, table, "Notes")
            });
        }

        return rows;
    }

    public IReadOnlyList<BuildingExcelRow> ReadBuildings(string filePath)
{
    using var workbook = OpenWorkbookSnapshot(filePath);

    var worksheet = workbook.Worksheet("Buildings");
    var table = worksheet.Table("Tabelle2");

    var rows = new List<BuildingExcelRow>();

    foreach (var row in table.DataRange.Rows())
    {
        rows.Add(new BuildingExcelRow
        {
            RowNumber = row.RowNumber(),

            InternalBuildingId = GetCellValue(row, table, "InternalBuildingId"),
            InternalCustomerId = GetCellValue(row, table, "InternalCustomerId"),
            FolderNumber = GetCellValue(row, table, "FolderNumber"),
            ProjectName = GetCellValue(row, table, "ProjectName"),
            OrganizationName = GetCellValue(row, table, "OrganizationName"),
            BuildingType = GetCellValue(row, table, "BuildingType"),
            Country = GetCellValue(row, table, "Country"),
            PostalCode = GetCellValue(row, table, "PostalCode"),
            City = GetCellValue(row, table, "City"),
            Street = GetCellValue(row, table, "Street"),
            HouseNumber = GetCellValue(row, table, "HouseNumber"),
            AddressAddition = GetCellValue(row, table, "AddressAddition"),
            ConstructionYear = GetCellValue(row, table, "ConstructionYear"),
            RenovationYear = GetCellValue(row, table, "RenovationYear"),
            UsageType = GetCellValue(row, table, "UsageType"),
            MainUsage = GetCellValue(row, table, "MainUsage"),
            AdditionalNote = GetCellValue(row, table, "AdditionalNote"),
            FloorArea = GetCellValue(row, table, "FloorArea"),
            HeatedFloorArea = GetCellValue(row, table, "HeatedFloorArea"),
            HeatedFloorAreaMainUsage = GetCellValue(row, table, "HeatedFloorAreaMainUsage"),
            HeatedFloorAreaSecondUsage = GetCellValue(row, table, "HeatedFloorAreaSecondUsage"),
            HeatConsumptionKWhPerYear = GetCellValue(row, table, "Heat consumption (kWh/year)"),
            HeatingCostsEuroPerYear = GetCellValue(row, table, "Heating costs (€/year)"),
            ElectricityConsumptionKWhPerYear = GetCellValue(row, table, "Electricity consumption (kWh/year)"),
            ElectricityCostsEuroPerYear = GetCellValue(row, table, "Electricity costs (€/year)"),
            HeatingType = GetCellValue(row, table, "HeatingType"),
            HasPV = GetCellValue(row, table, "HasPV"),
            HasBattery = GetCellValue(row, table, "HasBattery"),
            HwbCalculated = GetCellValue(row, table, "HWB (calculated)"),
            ElectricityConsumption = GetCellValue(row, table, "ElectricityConsumption"),
            HeatConsumption = GetCellValue(row, table, "HeatConsumption"),
            HotWaterConsumption = GetCellValue(row, table, "HotWaterConsumption"),
            EnergyReferenceYear = GetCellValue(row, table, "EnergyReferenceYear"),
            MainUsageSharePercent = GetCellValue(row, table, "MainUsageSharePercent"),
            SecondaryUsage = GetCellValue(row, table, "SecondaryUsage"),
            SecondaryUsageSharePercent = GetCellValue(row, table, "SecondaryUsageSharePercent"),
            Notes = GetCellValue(row, table, "Notes")
        });
    }

    return rows;
}

    private static XLWorkbook OpenWorkbookSnapshot(string filePath)
    {
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return new XLWorkbook(memoryStream);
    }

    private static string? GetCellValue(IXLRangeRow row, IXLTable table, string columnName)
    {
        var columnIndex = table.Fields
            .First(f => f.Name == columnName)
            .Index + 1;

        return row.Cell(columnIndex).GetFormattedString().Trim();
    }
}