using Microsoft.AspNetCore.Http;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class AnalyzeImportRequest
{
    public IFormFile ImportFile { get; set; } = default!;
}
