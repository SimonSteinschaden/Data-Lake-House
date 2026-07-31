namespace Enset.Domain.DataProducts;

/// <summary>
/// Beschreibt den Lebenszyklusstatus einer Datenproduktversion.
/// </summary>
public enum DataProductVersionStatus
{
    Draft = 0, // Represents a version of the data product that is still in the development or planning stage, and has not yet been finalized or released for use
    Generated = 1, //  Represents a version of the data product that has been generated but not yet validated 
    Validated = 2, // Represents a version of the data product that has undergone a validation process to ensure its accuracy, completeness, and compliance with predefined standards or criteria, indicating that it is ready for use or further processing
    Published = 3, // Represents a version of the data product that has been officially released and made available for use, indicating that it has passed all necessary validation and approval processes and is now considered the authoritative version for consumption
    Superseded = 4, // Represents a version of the data product that has been replaced or succeeded by a newer version, indicating that it is no longer the most current or relevant version for use
    Failed = 5 // Represents a version of the data product that has encountered errors or issues during its generation, validation, or publication processes, indicating that it is not suitable for use and may require further investigation or correction
}
