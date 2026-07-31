using Enset.Domain.Common;

namespace Enset.Domain.DataProducts;

/// <summary>
/// Beschreibt die Veröffentlichung einer Datenproduktversion im Marktplatz.
/// </summary>
public class DataProductMarketplacePublication : BaseEntity
{
    public Guid DataProductVersionId { get; set; }
    public DataProductVersion DataProductVersion { get; set; } = null!;

    public MarketplacePublicationStatus Status { get; set; }

    public DateTime? PublishedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }

    public string? LicenseCode { get; set; }
    public string? UsageTerms { get; set; }

    public bool RequiresApproval { get; set; } = true;
}
