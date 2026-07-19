public enum MeterReadingType
{
    Unknown = 0,
    Instantaneous = 1, // Represents a meter reading that captures the value of a parameter at a specific point in time, providing a snapshot of the measurement at that moment
    IntervalValue = 2, // Represents a meter reading that captures the value of a parameter over a defined time interval, providing an average or cumulative measurement for that period
    CumulativeValue = 3, // Represents a meter reading that captures the total accumulated value of a parameter over time, providing a running total or sum of the measurements since the start of the measurement period
    Calculated = 4, // Represents a meter reading that is derived from other readings or data sources, often using formulas or algorithms to calculate the value based on existing measurements
} 