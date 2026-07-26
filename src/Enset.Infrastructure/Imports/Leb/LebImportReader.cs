using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Leb;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Infrastructure.Imports.Leb;

public sealed class LebImportReader(
    LebWorkbookDto workbook,
    LebWorkbookMapper mapper,
    ImportMedium medium) : IImportReader
{
    public ImportWorkbook Read() => mapper.Map(workbook, medium);
}
