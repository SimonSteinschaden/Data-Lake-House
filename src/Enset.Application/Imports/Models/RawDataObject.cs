using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Models;

public class RawDataObject
{
    public Guid Id { get; set; }
    public RawDataObjectType Type { get; set; }
    public required string FilePath { get; init; }
}