using System.Linq;
using Content.Server.Corvax.Sponsors;
using Content.Shared.Corvax.Sponsors;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

public sealed class CheckSponsorSystem : EntitySystem
{
    [Dependency] private readonly SponsorsManager _sponsorsManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CheckUserSponsor>(SetSponsorStatus);
    }

    public bool CheckUser(string player)
    {
        EntityUid uid = EntityUid.Parse(player);
        _playerManager.TryGetSessionByEntity(uid, out var session);

        SponsorInfo? sponsorData;
        if (session != null)
            _sponsorsManager.TryGetInfo(session.UserId, out sponsorData);
        else
            return false;

        if (sponsorData != null && sponsorData.Tier > 0)
            return true;
        else
            return false;
    }

    private void SetSponsorStatus(CheckUserSponsor ev)
    {

        var isSponsor = CheckUser(ev.Player);
        var eve = new CheckedUserSponsor(isSponsor);
        RaiseNetworkEvent(eve);
    }
}
