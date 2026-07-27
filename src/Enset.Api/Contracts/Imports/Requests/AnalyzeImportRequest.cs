using Microsoft.AspNetCore.Http;
using Enset.Application.Imports.Enums;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class AnalyzeImportRequest
{
    public IFormFile ImportFile { get; set; } = default!;
    public string SourceType { get; set; } =
        nameof(ImportSourceType.CRM_Excel);
    public ImportMedium? Medium { get; set; }
    public string? DefaultMeterNumber { get; set; }
    public Guid? TargetMeteringPointId { get; set; }
}
