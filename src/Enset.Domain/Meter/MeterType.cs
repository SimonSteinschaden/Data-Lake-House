public enum MeterType
{
    Unknown = 0,

    Physical = 1, // Represents a physical meter device installed in the field

    Virtual = 2, // Represents a virtual meter that aggregates or calculates data from other meters or sources

    Calculated = 3, // Represents a calculated meter that derives its readings from other meters or data sources, often using formulas or algorithms

    Aggregated = 4 // Represents an aggregated meter that combines readings from multiple meters to provide a consolidated view of energy consumption or production
}