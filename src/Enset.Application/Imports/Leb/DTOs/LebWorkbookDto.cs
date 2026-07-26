namespace Enset.Application.Imports.Leb.DTOs;

public sealed class LebWorkbookDto
{
    public IReadOnlyList<LebSourceColumn> Columns { get; init; } = [];
    public IReadOnlyList<LebRowDto> Rows { get; init; } = [];
}
