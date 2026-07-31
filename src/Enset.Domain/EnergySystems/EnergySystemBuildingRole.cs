namespace Enset.Domain.Energy;

public enum EnergySystemBuildingRole
{
    Unknown = 0,
    LocatedAt = 1,
    Supplies = 2,
    GeneratesFor = 3,
    StoresEnergyFor = 4,
    SharedSystem = 5,
    Other = 99
}