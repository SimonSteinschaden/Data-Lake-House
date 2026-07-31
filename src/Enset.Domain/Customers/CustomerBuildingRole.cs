namespace Enset.Domain.Customers;

public enum CustomerBuildingRole
{
    Unknown = 0,
    Owner = 1,
    Operator = 2,
    Tenant = 3,
    PropertyManager = 4,
    EnergyManager = 5,
    ContractingPartner = 6,
    Other = 99
}