namespace Enset.Domain.DataProducts;

/// <summary>
/// Beschreibt die fachliche Rolle eines Kunden in Bezug auf ein Datenprodukt.
/// </summary>
public enum DataProductCustomerRole
{
    Owner = 0, // Represents the primary owner of the data product, responsible for its management and decision-making regarding its use and distribution
    DataProvider = 1, // Represents an entity that supplies data to the data product, contributing to its content and ensuring the quality and accuracy of the data provided
    AuthorizedConsumer = 2, // Represents an entity that has been granted permission to access and use the data product, typically for specific purposes or within certain constraints defined by the owner or provider
    Beneficiary = 3 // Represents an entity that benefits from the data product, either through insights, analytics, or other value derived from the data, without necessarily having ownership or direct access to the underlying data
}
