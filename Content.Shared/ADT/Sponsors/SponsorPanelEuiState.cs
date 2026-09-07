using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorPanelEuiState : EuiStateBase
{
    public bool HasPermission;

    public SponsorTier[] Tiers = Array.Empty<SponsorTier>();

    public Guid? PlayerId;

    public string PlayerName = string.Empty;

    public SponsorGrant[] Grants = Array.Empty<SponsorGrant>();

    public SponsorBenefits? Resolved;

    public string Status = string.Empty;
}

public static class SponsorPanelEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class LookupPlayer : EuiMessageBase
    {
        public string Query = string.Empty;
    }

    [Serializable, NetSerializable]
    public sealed class SaveTier : EuiMessageBase
    {
        public SponsorTier Tier = new();
    }

    [Serializable, NetSerializable]
    public sealed class DeleteTier : EuiMessageBase
    {
        public int TierId;
    }

    [Serializable, NetSerializable]
    public sealed class SaveGrant : EuiMessageBase
    {
        public SponsorGrant Grant = new();
    }

    [Serializable, NetSerializable]
    public sealed class RevokeGrant : EuiMessageBase
    {
        public int GrantId;
    }
}
