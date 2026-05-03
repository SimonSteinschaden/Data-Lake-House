public class CalculationResult : BaseEntity
{
    public KPIType KPIType { get; set; }

    public ScopeLevel ScopeLevel { get; set; }

    public Guid ScopeId { get; set; }

    public decimal Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}