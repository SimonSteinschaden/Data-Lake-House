using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Abstractions;

public interface IImportReader
{
    ImportWorkbook Read();
}