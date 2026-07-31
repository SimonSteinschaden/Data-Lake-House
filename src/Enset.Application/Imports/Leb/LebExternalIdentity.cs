namespace Enset.Application.Imports.Leb;

public static class LebExternalIdentity
{
    public const string SourceSystem = "Landesenergiebuchhaltung";

    public static string Municipality(string municipalityId) =>
        $"LEB:GEM:{Normalize(municipalityId)}";

    public static string Building(string municipalityId, string buildingId) =>
        $"{Municipality(municipalityId)}:GEB:{Normalize(buildingId)}";

    public static string Meter(
        string municipalityId,
        string buildingId,
        string meterId) =>
        $"{Building(municipalityId, buildingId)}:Z:{Normalize(meterId)}";

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
