using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorLegacyTier
{
    public int LegacyTier;

    public int? TierId;

    public DateTime? LastSeen;

    public int OnlinePlayers;
}
