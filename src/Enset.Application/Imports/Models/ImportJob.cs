using Enset.Domain.Common;
using Enset.Domain.Projects;
using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Models;

public class ImportJob : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ImportSourceType SourceType { get; set; }

    public ImportStatus Status { get; set; }

}