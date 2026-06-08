namespace Enset.Application.Imports.Reports;

public class ImportReport
{
    public int CustomerCount { get; set; }

    public int BuildingCount { get; set; }

    public List<string> Errors { get; } = [];

    public List<string> Warnings { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;
}