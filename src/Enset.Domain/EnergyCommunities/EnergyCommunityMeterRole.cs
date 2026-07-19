namespace Enset.Domain.EnergyCommunities;

public enum EnergyCommunityMeterRole
{
    Unknown = 0,
    Consumption = 1,
    Generation = 2,
    Storage = 3,
    Bidirectional = 4,
    CommunityBoundary = 5,
    Other = 99
}