namespace Enset.Domain.EnergyCommunities;

/// <summary>
/// Geographic or grid-topological scope of a shared-energy arrangement.
/// The scope affects eligibility, network-fee treatment and validation.
/// </summary>
public enum EnergyCommunityScope
{
    Unknown = 0,

    /// <summary>
    /// Shared private/common electrical installation or common connection.
    /// Typical scope for a GEA or self-supply installation.
    /// </summary>
    SharedConnection = 1,

    /// <summary>
    /// Local near-area determined by the relevant low-voltage
    /// network topology.
    /// </summary>
    LocalNearArea = 2,

    /// <summary>
    /// Regional near-area determined by the relevant medium-voltage
    /// network topology.
    /// </summary>
    RegionalNearArea = 3,

    /// <summary>
    /// Participation outside a local or regional near-area,
    /// but within Austria.
    /// </summary>
    Nationwide = 4,

    /// <summary>
    /// Custom or analytical boundary not directly derived
    /// from the regulated network topology.
    /// </summary>
    Custom = 99
}