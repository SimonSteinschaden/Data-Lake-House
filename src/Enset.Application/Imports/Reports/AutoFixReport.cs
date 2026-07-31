namespace Enset.Application.Imports.Reports;

public class AutoFixReport
{
    public int GeneratedCustomerIds { get; set; }

    public int GeneratedBuildingIds { get; set; }

    public List<string> Messages { get; } = [];
}