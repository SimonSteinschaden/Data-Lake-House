using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Excel;

public sealed class ExcelWorkbookReader : IExcelReader, IExcelWorkbookReader
{
    private readonly IImportLogger? _logger;

    public ExcelWorkbookReader(IImportLogger? logger = null)
    {
        _logger = logger;
    }

    public ImportWorkbook Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new InvalidImportFileException("The uploaded file stream is not readable.");

        try
        {
            using var memoryStream = CopyToSnapshot(stream);
            using var workbook = new XLWorkbook(memoryStream);
            var hasMeters = workbook.TryGetWorksheet("Meters", out _);
            var hasMeterReadings = workbook.TryGetWorksheet("MeterReadings", out _);

            if (hasMeters != hasMeterReadings)
            {
                var missingWorksheet = hasMeters ? "MeterReadings" : "Meters";
                throw new InvalidImportFileException(
                    $"Worksheet '{missingWorksheet}' is missing.");
            }

            return new ImportWorkbook
            {
                Customers = ReadCustomers(workbook),
                Buildings = ReadBuildings(workbook),
                Meters = hasMeters ? ReadMeters(workbook) : [],
                MeterReadings = hasMeterReadings ? ReadMeterReadings(workbook) : []
            };
        }
        catch (InvalidImportFileException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FileFormatException or
            InvalidDataException or
            OpenXmlPackageException)
        {
            throw new InvalidImportFileException(
                "The uploaded file is not a valid Excel workbook.",
                exception);
        }
    }

    public IReadOnlyList<CustomerExcelRow> ReadCustomers(string filePath)
    {
        using var workbook = OpenWorkbookSnapshot(filePath);
        return ReadCustomers(workbook);
    }

    public IReadOnlyList<BuildingExcelRow> ReadBuildings(string filePath)
    {
        using var workbook = OpenWorkbookSnapshot(filePath);
        return ReadBuildings(workbook);
    }

    private IReadOnlyList<CustomerExcelRow> ReadCustomers(XLWorkbook workbook)
    {
        var sheet = OpenSheet(workbook, "Customers", "ExternalCustomerId", "CompanyName");
        return sheet.Rows.Select(row => new CustomerExcelRow
        {
            RowNumber = row.RowNumber(),
            InternalCustomerId = Get(row, sheet.Headers, "ExternalCustomerId", "InternalCustomerId"),
            OrganizationName = Get(row, sheet.Headers, "OrganizationName", "CompanyName"),
            FirstName = Get(row, sheet.Headers, "FirstName"),
            LastName = Get(row, sheet.Headers, "LastName"),
            VatNumber = Get(row, sheet.Headers, "VatNumber"),
            CompanyRegistrationNumber = Get(row, sheet.Headers, "CompanyRegistrationNumber"),
            ContactPerson = Get(row, sheet.Headers, "ContactPerson"),
            Street = Get(row, sheet.Headers, "Street"),
            HouseNumber = Get(row, sheet.Headers, "HouseNumber"),
            PostalCode = Get(row, sheet.Headers, "PostalCode"),
            City = Get(row, sheet.Headers, "City"),
            Country = Get(row, sheet.Headers, "Country"),
            Email = Get(row, sheet.Headers, "Email"),
            PhoneNumber = Get(row, sheet.Headers, "Phone", "PhoneNumber")
        }).ToList();
    }

    private IReadOnlyList<BuildingExcelRow> ReadBuildings(XLWorkbook workbook)
    {
        var sheet = OpenSheet(workbook, "Buildings", "ExternalBuildingId", "ExternalCustomerId");
        return sheet.Rows.Select(row => new BuildingExcelRow
        {
            RowNumber = row.RowNumber(),
            InternalBuildingId = Get(row, sheet.Headers, "ExternalBuildingId", "InternalBuildingId"),
            InternalCustomerId = Get(row, sheet.Headers, "ExternalCustomerId", "InternalCustomerId"),
            BuildingName = Get(row, sheet.Headers, "BuildingName", "ProjectName"),
            ProjectName = Get(row, sheet.Headers, "ProjectName", "BuildingName"),
            Street = Get(row, sheet.Headers, "Street"),
            HouseNumber = Get(row, sheet.Headers, "HouseNumber"),
            PostalCode = Get(row, sheet.Headers, "PostalCode"),
            City = Get(row, sheet.Headers, "City"),
            Country = Get(row, sheet.Headers, "Country"),
            BuildingType = Get(row, sheet.Headers, "BuildingType")
        }).ToList();
    }

    private IReadOnlyList<MeterExcelRow> ReadMeters(XLWorkbook workbook)
    {
        var sheet = OpenSheet(workbook, "Meters", "MeterNumber");
        return sheet.Rows.Select(row => new MeterExcelRow
        {
            RowNumber = row.RowNumber(),
            MeterNumber = Get(row, sheet.Headers, "MeterNumber"),
            FileName = Get(row, sheet.Headers, "FileName"),
            ProfileName = Get(row, sheet.Headers, "ProfileName"),
            Unit = Get(row, sheet.Headers, "Unit"),
            ExternalCustomerId = Get(row, sheet.Headers, "ExternalCustomerId"),
            ExternalBuildingId = Get(row, sheet.Headers, "ExternalBuildingId")
        }).ToList();
    }

    private IReadOnlyList<MeterReadingExcelRow> ReadMeterReadings(XLWorkbook workbook)
    {
        var sheet = OpenSheet(workbook, "MeterReadings", "MeterNumber", "Timestamp", "Value");
        return sheet.Rows.Select(row => new MeterReadingExcelRow
        {
            RowNumber = row.RowNumber(),
            MeterNumber = Get(row, sheet.Headers, "MeterNumber"),
            Timestamp = Get(row, sheet.Headers, "Timestamp"),
            Value = Get(row, sheet.Headers, "Value"),
            Unit = Get(row, sheet.Headers, "Unit"),
            QualityFlag = Get(row, sheet.Headers, "QualityFlag")
        }).ToList();
    }

    private SheetData OpenSheet(
        XLWorkbook workbook,
        string worksheetName,
        params string[] requiredHeaders)
    {
        if (!workbook.TryGetWorksheet(worksheetName, out var worksheet))
            throw new InvalidImportFileException($"Worksheet '{worksheetName}' is missing.");

        _logger?.Info($"Worksheet '{worksheetName}' found.");

        var table = worksheet.Tables.FirstOrDefault();
        var range = table?.AsRange() ?? worksheet.RangeUsed();
        if (range is null)
            throw new InvalidImportFileException($"Worksheet '{worksheetName}' is empty.");

        var headerRow = table?.HeadersRow() ?? range.FirstRow();
        var headers = headerRow.Cells()
            .Select((cell, index) => new { Name = cell.GetFormattedString().Trim(), Index = index + 1 })
            .Where(header => !string.IsNullOrWhiteSpace(header.Name))
            .GroupBy(header => header.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

        foreach (var requiredHeader in requiredHeaders)
        {
            if (!headers.ContainsKey(requiredHeader) && !HasLegacyAlias(headers, requiredHeader))
                throw new InvalidImportFileException(
                    $"Column '{requiredHeader}' is missing in worksheet '{worksheetName}'.");
        }

        var rows = (table?.DataRange?.Rows() ?? range.Rows().Skip(1))
            .Where(row => row.Cells().Any(cell => !cell.IsEmpty()))
            .ToList();

        return new SheetData(headers, rows);
    }

    private static bool HasLegacyAlias(
        IReadOnlyDictionary<string, int> headers,
        string requiredHeader) => requiredHeader switch
    {
        "ExternalCustomerId" => headers.ContainsKey("InternalCustomerId"),
        "ExternalBuildingId" => headers.ContainsKey("InternalBuildingId"),
        "CompanyName" => headers.ContainsKey("OrganizationName"),
        _ => false
    };

    private static string? Get(
        IXLRangeRow row,
        IReadOnlyDictionary<string, int> headers,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(name, out var columnIndex))
                return row.Cell(columnIndex).GetFormattedString().Trim();
        }

        return null;
    }

    private static MemoryStream CopyToSnapshot(Stream stream)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        if (memoryStream.Length == 0)
        {
            memoryStream.Dispose();
            throw new InvalidImportFileException("The uploaded Excel file is empty.");
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static XLWorkbook OpenWorkbookSnapshot(string filePath)
    {
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        return new XLWorkbook(CopyToSnapshot(fileStream));
    }

    private sealed record SheetData(
        IReadOnlyDictionary<string, int> Headers,
        IReadOnlyList<IXLRangeRow> Rows);
}
