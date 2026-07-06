namespace Enset.Application.Imports.Models;

public class CustomerExcelRow
{
    public int RowNumber { get; set; }

    public string? InternalCustomerId { get; set; } //TODO: SQL Injection Prevention: Validate and sanitize the input to prevent SQL injection attacks.
    public string? FolderNumber { get; set; }
    public string? EKutCustomer { get; set; }
    public string? ProjectName { get; set; }
    public string? OrganizationName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? AddressAddtion { get; set; }
    public string? Prosumer { get; set; }
    public string? HasLoadProfile { get; set; }
    public string? BuildingRenovationInterest { get; set; }
    public string? EnergyCommunityInterest { get; set; }
    public string? MobilityInterest { get; set; }
    public string? PlusEnergyInterest { get; set; }
    public string? Subsidies { get; set; }
    public string? LastContact { get; set; }
    public string? Notes { get; set; }
}