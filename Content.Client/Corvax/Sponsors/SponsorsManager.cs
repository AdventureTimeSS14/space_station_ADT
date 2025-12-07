using System.Diagnostics.CodeAnalysis;
using Content.Shared.Corvax.Sponsors;
using Robust.Shared.Network;

namespace Content.Client.Corvax.Sponsors;

public sealed class SponsorsManager : ISponsorsManager // Ganimed-Sponsors
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private SponsorInfo? _info;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(msg => _info = msg.Info);
    }

    public bool TryGetInfo([NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        sponsor = _info;
        return _info != null;
    }

    // Ganimed-Sponsors start
    bool ISponsorsManager.TryGetInfo(Robust.Shared.Network.NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        sponsor = null;
        return false;
    }
    // Ganimed-Sponsors end
}