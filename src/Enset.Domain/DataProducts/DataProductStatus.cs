namespace Enset.Domain.DataProducts;

public enum DataProductStatus
{
    Draft = 0, // Represents a data product that is still in the development or planning stage, and has not yet been finalized or released for use
    Active = 1, // Represents a data product that is currently active and available for use
    Inactive = 2, // Represents a data product that is not currently active but may be reactivated at a later time
    Archived = 3 // Represents a data product that has been archived and is no longer actively used or maintained
}