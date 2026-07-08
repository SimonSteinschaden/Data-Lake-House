using Microsoft.AspNetCore.Http;

namespace Enset.Api.Contracts.Imports.Requests;

public sealed class AnalyzeImportFormRequest
{
    public IFormFile File { get; set; } = default!;
}
