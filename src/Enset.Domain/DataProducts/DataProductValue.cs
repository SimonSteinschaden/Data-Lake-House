using Enset.Domain.Common;
using Enset.Domain.Data;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Speichert einen typisierten Einzelwert einer Datenproduktversion.
/// </summary>
public class DataProductValue : BaseEntity
{
    public Guid DataProductVersionId { get; set; }

    public DataProductVersion DataProductVersion { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public decimal? NumericValue { get; set; }

    public string? TextValue { get; set; }

    public bool? BooleanValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public string? Unit { get; set; }

    public DataQuality Quality { get; set; }
}
