namespace Enset.Domain.Data;

public enum DataQuality
{
    Unknown = 0,
    Measured = 1,
    Validated = 2,
    Estimated = 3,
    Interpolated = 4,
    Calculated = 5,
    Invalid = 6,
    Missing = 7
}
