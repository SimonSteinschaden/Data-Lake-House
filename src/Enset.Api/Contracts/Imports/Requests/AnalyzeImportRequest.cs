using Microsoft.AspNetCore.Http;
using Enset.Application.Imports.Enums;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class AnalyzeImportRequest
{
    public IFormFile ImportFile { get; set; } = default!;
    public ImportSourceType SourceType { get; set; } = ImportSourceType.EnsetWorkbook;
    public ImportMedium? Medium { get; set; }
    public string? DefaultMeterNumber { get; set; }
}
