using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;

namespace Enset.Application.Imports.WriteGate;

public class ImportWriteContext
{
    public ImportDecisionType Decision { get; init; }

    public bool UserConfirmed { get; init; }

    public IReadOnlyCollection<ImportIssue> Issues { get; init; }
        = [];

    public IReadOnlyCollection<CustomerImportDto> Customers { get; init; }
        = [];
}
