namespace Enset.Application.Imports.DTOs;

public class CustomerImportDto
{
    // Externe Referenzen
    public string? ExternalCustomerId { get; set; } //TODO: SQL Injection prevention, Input validation, restliche Inputs ergänzen - zb: Notes

    // Unternehmen
    public string? CompanyName { get; set; }
    public string? VatNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }

    // Ansprechpartner
    public string? ContactPerson { get; set; }

    // Adresse
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

    // Kommunikation
    public string? Email { get; set; }
    public string? Phone { get; set; }

}