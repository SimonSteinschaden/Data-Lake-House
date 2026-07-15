using Microsoft.AspNetCore.Http;

namespace Enset.Api.Contracts;

public sealed class AnalyzeImportFormRequest
{
    public IFormFile File { get; set; } = default!;
}