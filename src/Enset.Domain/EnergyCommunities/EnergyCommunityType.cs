namespace Enset.Domain.EnergyCommunities;

/// <summary>
/// Legal and organizational model used for shared energy.
/// </summary>
public enum EnergyCommunityType
{
    Unknown = 0,

    /// <summary>
    /// Gemeinschaftliche Erzeugungsanlage.
    /// Typically restricted to participants using the same connection
    /// or private/common electrical installation.
    /// </summary>
    GemeinschaftlicheErzeugungsanlage = 1,

    /// <summary>
    /// Erneuerbare-Energie-Gemeinschaft.
    /// Limited to renewable energy and subject to EEG participation rules.
    /// </summary>
    RenewableEnergyCommunity = 2,

    /// <summary>
    /// Bürgerenergiegemeinschaft.
    /// Primarily electricity; may operate across wider geographic areas.
    /// </summary>
    CitizenEnergyCommunity = 3,

    /// <summary>
    /// TODO Phase 5+:
    /// Implement contractual parties, pricing,
    /// automatic settlement and billing.
    /// </summary>
    PeerToPeer = 4,

    /// <summary>
    /// New ElWG concept for joint use behind or through a shared
    /// customer installation. Exact migration mapping from existing
    /// GEA structures must be reviewed during ElWG implementation.
    /// </summary>
    SelfSupplyInstallation = 5,

    Custom = 99
}