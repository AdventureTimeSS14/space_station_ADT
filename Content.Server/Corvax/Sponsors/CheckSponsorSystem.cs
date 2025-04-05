using Content.Server.Corvax.Sponsors;
using Content.Shared.Corvax.Sponsors;
using Robust.Server.Player;

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
        if (!EntityUid.TryParse(player, out var uid))
            return false;

        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return false;

        if (!_sponsorsManager.TryGetInfo(session.UserId, out var sponsorData))
            return false;

        // ADT-SPONSORS
        return sponsorData.Tier > 0 || sponsorData.AllowJob;
        // ADT-SPONSORS
    }

    private void SetSponsorStatus(CheckUserSponsor ev)
    {

        var isSponsor = CheckUser(ev.Player);
        var eve = new CheckedUserSponsor(isSponsor);
        RaiseNetworkEvent(eve);
    }
}
