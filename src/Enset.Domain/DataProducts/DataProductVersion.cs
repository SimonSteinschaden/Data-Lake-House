using Enset.Domain.Common;
using Enset.Domain.Data;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Repräsentiert eine erzeugte und fachlich versionierte Ausgabe eines Datenprodukts.
/// </summary>
public class DataProductVersion : BaseEntity
{
    public Guid DataProductId { get; set; }

    public DataProduct DataProduct { get; set; } = null!;

    public int VersionNumber { get; set; }

    public DataProductVersionStatus Status { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime? InputPeriodFrom { get; set; }

    public DateTime? InputPeriodTo { get; set; }

    public DataQuality Quality { get; set; }

    public Guid? GenerationRunId { get; set; }

    public DataProductGenerationRun? GenerationRun { get; set; }

    public ICollection<DataProductValue> Values { get; set; }
        = new List<DataProductValue>();
}
