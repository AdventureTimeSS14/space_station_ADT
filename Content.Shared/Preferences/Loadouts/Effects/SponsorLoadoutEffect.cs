using System.Diagnostics.CodeAnalysis;
using Content.Shared.Corvax.Sponsors;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Разрешает выбирать лодаут только спонсорам с опциональным ограничением по Tier.
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
    [DataField("tier")]
    public int? RequiredTier { get; private set; }

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (session == null)
            return true;

        var net = collection.Resolve<INetManager>();
        var isSponsor = false;
        SponsorInfo? info = null;

        if (net.IsClient)
        {
            if (collection.TryResolveType<ISponsorsManager>(out var sponsorsClient))
                isSponsor = sponsorsClient.TryGetInfo(out info) && info != null;
        }
        else
        {
            if (collection.TryResolveType<ISponsorsManager>(out var sponsorsServer))
                isSponsor = sponsorsServer.TryGetInfo(session.UserId, out info) && info != null;
        }

        if (!isSponsor || info == null)
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
            return false;
        }

        if (RequiredTier.HasValue)
        {
            var userTier = info.Tier ?? 0;
            if (userTier < RequiredTier.Value)
            {
                reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString(
                    "loadout-sponsor-tier-restriction",
                    ("requiredTier", RequiredTier.Value),
                    ("userTier", userTier)));
                return false;
            }
        }

        return true;
    }
}