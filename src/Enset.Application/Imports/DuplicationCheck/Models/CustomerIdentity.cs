namespace Enset.Application.Imports.DuplicationCheck.Models;

public class CustomerIdentity
{
    public string NormalizedCompanyName { get; set; } = string.Empty;

    public string NormalizedStreet { get; set; } = string.Empty;

    public string NormalizedHouseNumber { get; set; } = string.Empty;

    public string NormalizedPostalCode { get; set; } = string.Empty;

    public string NormalizedCity { get; set; } = string.Empty;

    public string? VatNumber { get; set; }

    public string? CompanyRegistrationNumber { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string ExactKey { get; set; } = string.Empty;
}