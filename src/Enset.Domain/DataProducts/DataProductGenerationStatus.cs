namespace Enset.Domain.DataProducts;

public enum DataProductGenerationStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    CompletedWithWarnings = 3,
    Failed = 4,
    Cancelled = 5
}