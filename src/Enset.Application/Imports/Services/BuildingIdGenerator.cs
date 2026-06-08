namespace Enset.Application.Imports.Services;

public static class BuildingIdGenerator
{
    public static string Generate()
    {
        return $"BLDG-{Guid.NewGuid():N}";
    }
}