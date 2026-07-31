using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Enset.Application.ObjectAnalytics;
using Enset.Application.Quality;
using Enset.Application.Reporting;

namespace Enset.Infrastructure.Reporting;

public sealed class FileReportService(
    string rootPath,
    IObjectAnalyticsService analytics,
    IHierarchicalQualityAssessmentService assessments,
    TimeProvider timeProvider)
    : IReportService
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly string root = Initialize(rootPath);
    private readonly JsonSerializerOptions json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public IReadOnlyList<ReportDefinition> Definitions() =>
        Enum.GetValues<ReportType>().Select(type => new ReportDefinition(
            type,
            Title(type),
            "Versionierter Bericht auf Basis des Object Analytics Data Product.",
            ["pdf", "xlsx", "json"])).ToArray();

    public async Task<IReadOnlyList<ReportInstance>> List(
        CancellationToken cancellationToken)
    {
        var result = new List<ReportInstance>();
        foreach (var path in Directory.EnumerateFiles(root, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var item = await JsonSerializer.DeserializeAsync<ReportInstance>(
                stream, json, cancellationToken);
            if (item is not null)
                result.Add(item);
        }
        return result.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }

    public async Task<ReportInstance> Create(
        CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient))
            throw new ArgumentException("A report recipient is required.");
        var product = await analytics.Analyze(
            request.BuildingId,
            new ObjectSearchQuery(
                null, null, null, null, null,
                request.FromUtc, request.ToUtc),
            cancellationToken) ??
            throw new KeyNotFoundException(
                $"Building {request.BuildingId} was not found.");
        var quality = (await assessments.AssessBuildings(
            [request.BuildingId], cancellationToken))
            .GetValueOrDefault(request.BuildingId);
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var existing = await List(cancellationToken);
            var version = existing.Where(x =>
                    x.BuildingId == request.BuildingId &&
                    x.Type == request.Type)
                .Select(x => x.Version).DefaultIfEmpty().Max() + 1;
            var instance = new ReportInstance(
                Guid.NewGuid(),
                request.Type,
                request.BuildingId,
                product.BuildingName,
                request.FromUtc,
                request.ToUtc,
                version,
                timeProvider.GetUtcNow().UtcDateTime,
                request.Recipient.Trim(),
                ReportReleaseStatus.Draft,
                product.Quality.Level.ToString(),
                Suitability(product),
                product)
            {
                GoldProgressPercentage = quality?.Assessment.GoldProgress.Percentage ?? 0,
                BronzeCount = quality?.Assessment.GoldProgress.BronzeCount ?? 0,
                SilverCount = quality?.Assessment.GoldProgress.SilverCount ?? 0,
                GoldCount = quality?.Assessment.GoldProgress.GoldCount ?? 0,
                InventoryDeclarationStatus = quality?.InventoryDeclarationStatus ?? "Nicht bestätigt",
                AnalysisVersions = quality?.MeterAssessments
                    .Where(x => x.AnalysisVersion != null)
                    .Select(x => x.AnalysisVersion!).Distinct().ToArray() ?? [],
                OpenIssueCount = quality?.MeterAssessments.Sum(x => x.OpenIssueCount) ?? 0,
                BlockingReasons = quality?.Assessment.BlockingReasons ?? [],
                ConfirmationStatus = quality?.Assessment.OverallQualityLevel.ToString() == "Gold"
                    ? "Bestätigt" : "Nicht bestätigt"
            };
            await using var stream = new FileStream(
                PathFor(instance.ReportId),
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);
            await JsonSerializer.SerializeAsync(
                stream, instance, json, cancellationToken);
            return instance;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async Task<ReportInstance?> Get(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var path = PathFor(reportId);
        if (!File.Exists(path))
            return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ReportInstance>(
            stream, json, cancellationToken);
    }

    public Task<ReportInstance?> Release(
        Guid reportId, string releasedBy, CancellationToken cancellationToken) =>
        ChangeStatus(reportId, ReportReleaseStatus.Released, releasedBy, cancellationToken);

    public Task<ReportInstance?> Archive(
        Guid reportId, CancellationToken cancellationToken) =>
        ChangeStatus(reportId, ReportReleaseStatus.Archived, null, cancellationToken);

    private async Task<ReportInstance?> ChangeStatus(
        Guid reportId, ReportReleaseStatus status, string? releasedBy,
        CancellationToken cancellationToken)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var report = await Get(reportId, cancellationToken);
            if (report is null)
                return null;
            var updated = report with
            {
                ReleaseStatus = status,
                ReleasedBy = status == ReportReleaseStatus.Released ? releasedBy : report.ReleasedBy,
                ReleasedAtUtc = status == ReportReleaseStatus.Released
                    ? timeProvider.GetUtcNow().UtcDateTime : report.ReleasedAtUtc
            };
            await using var stream = new FileStream(
                PathFor(reportId),
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);
            await JsonSerializer.SerializeAsync(
                stream, updated, json, cancellationToken);
            return updated;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async Task<RenderedReport?> Export(
        Guid reportId,
        string format,
        CancellationToken cancellationToken)
    {
        var report = await Get(reportId, cancellationToken);
        if (report is null)
            return null;
        return format.Trim().ToLowerInvariant() switch
        {
            "json" => new(
                Name(report, "json"),
                "application/json",
                JsonSerializer.SerializeToUtf8Bytes(report, json)),
            "xlsx" => new(
                Name(report, "xlsx"),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Excel(report)),
            "pdf" => new(
                Name(report, "pdf"),
                "application/pdf",
                Pdf(report)),
            _ => throw new ArgumentException(
                "Supported formats are pdf, xlsx and json.")
        };
    }

    private static byte[] Excel(ReportInstance report)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");
        var rows = Summary(report).ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            summary.Cell(index + 1, 1).Value = rows[index].Key;
            summary.Cell(index + 1, 2).Value = rows[index].Value;
        }
        summary.Columns().AdjustToContents();
        var monthly = workbook.Worksheets.Add("Monthly consumption");
        monthly.Cell(1, 1).Value = "Timestamp";
        monthly.Cell(1, 2).Value = "Value (kWh)";
        for (var index = 0;
             index < report.Product.MonthlyConsumption.Count;
             index++)
        {
            monthly.Cell(index + 2, 1).Value =
                report.Product.MonthlyConsumption[index].Timestamp;
            monthly.Cell(index + 2, 2).Value =
                report.Product.MonthlyConsumption[index].Value;
        }
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static byte[] Pdf(ReportInstance report)
    {
        var text = string.Join(" | ", Summary(report)
            .Select(x => $"{x.Key}: {x.Value}"));
        text = text.Replace("\\", "\\\\")
            .Replace("(", "\\(").Replace(")", "\\)");
        if (text.Length > 1500)
            text = text[..1500];
        var body = $"BT /F1 9 Tf 36 790 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
            "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(body)} >>\nstream\n" +
            body + "\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var output = new MemoryStream();
        void Write(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            output.Write(bytes);
        }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {objects.Length + 1}\n");
        Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\n" +
              $"startxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static IEnumerable<KeyValuePair<string, string>> Summary(
        ReportInstance report)
    {
        yield return new("Title", Title(report.Type));
        yield return new("Object", report.BuildingName);
        yield return new("Version", report.Version.ToString());
        yield return new("Created", report.CreatedAtUtc.ToString("O"));
        yield return new("Recipient", report.Recipient);
        yield return new("Quality", report.QualityLevel);
        yield return new("Suitability", report.Suitability);
        yield return new("Consumption",
            Format(report.Product.TotalConsumption));
        yield return new("Production",
            Format(report.Product.EnergyProduction));
        yield return new("Costs", Format(report.Product.EnergyCosts));
        yield return new("CO2", Format(report.Product.Co2Emissions));
        yield return new("Peak load", Format(report.Product.PeakLoad));
        yield return new("Sources",
            "Canonical Snapshot / Object Analytics Data Product");
    }

    private static string Format(AnalyticsValue value) =>
        value.Value.HasValue
            ? $"{value.Value.Value.ToString(
                "0.##", CultureInfo.InvariantCulture)} {value.Unit}"
            : "nicht verfügbar";

    private static string Suitability(ObjectAnalyticsProduct product) =>
        $"Benchmark={product.Suitability.Benchmark}; " +
        $"ISO50001={product.Suitability.Iso50001}; " +
        $"LEB={product.Suitability.Leb}";

    private static string Name(ReportInstance report, string extension) =>
        $"{report.Type}-{report.BuildingId:D}-v{report.Version}.{extension}";

    private string PathFor(Guid id) => Path.Combine(root, $"{id:N}.json");

    private static string Initialize(string path)
    {
        var result = Path.GetFullPath(path);
        Directory.CreateDirectory(result);
        return result;
    }

    private static string Title(ReportType type) => type switch
    {
        ReportType.ObjectEnergy => "Objekt-Energiebericht",
        ReportType.AnnualEnergy => "Jahresenergiebericht",
        ReportType.Consumption => "Verbrauchsbericht",
        ReportType.Cost => "Kostenbericht",
        ReportType.Co2 => "CO₂-Bericht",
        ReportType.LoadProfile => "Lastprofilbericht",
        ReportType.EnergySystems => "Anlagenbericht",
        ReportType.DataQuality => "Datenqualitätsbericht",
        ReportType.PortfolioComparison => "Portfoliovergleich",
        ReportType.Iso50001 => "ISO-50001-Auswertung",
        _ => "Landesenergiebuchhaltungsbericht"
    };
}
