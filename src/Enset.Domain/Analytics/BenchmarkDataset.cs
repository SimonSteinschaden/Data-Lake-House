public class BenchmarkDataset : BaseEntity
{
    public ScopeLevel ScopeLevel { get; set; }

    public string Region { get; set; } = string.Empty;

    public BuildingCategory BuildingCategory { get; set; }

    public string YearRange { get; set; } = string.Empty;

    public decimal AvgConsumption { get; set; }

    public int SampleSize { get; set; }
}