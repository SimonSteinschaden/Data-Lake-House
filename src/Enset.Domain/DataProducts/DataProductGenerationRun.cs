using Enset.Domain.Common;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Protokolliert Ausführung, Parameter und Ergebnis eines Generierungslaufs.
/// </summary>
public class DataProductGenerationRun : BaseEntity
{
    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DataProductGenerationStatus Status { get; set; }

    public string GeneratorName { get; set; } = string.Empty;

    public string GeneratorVersion { get; set; } = string.Empty;

    public string? InputHash { get; set; }

    public string? ParameterJson { get; set; }

    public string? Warnings { get; set; }

    public string? ErrorMessage { get; set; }

    public string? TriggeredBy { get; set; }

    public ICollection<DataProductVersion> GeneratedVersions { get; set; }
        = new List<DataProductVersion>();
}
