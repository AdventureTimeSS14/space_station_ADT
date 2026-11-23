using System.Diagnostics.CodeAnalysis;
using Content.Shared.Corvax.Sponsors;
using Robust.Shared.Network;

namespace Content.Client.Corvax.Sponsors;

public sealed class SponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly Robust.Shared.Configuration.IConfigurationManager _cfg = default!; // ADT-Tweak

    private SponsorInfo? _info;
    private bool _legacyBridge = true; // ADT-Tweak

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(msg => _info = msg.Info);
        _cfg.OnValueChanged(Content.Shared.ADT.Sponsors.SponsorCVars.LegacyBridge, v => _legacyBridge = v, true); // ADT-Tweak
    }

    public bool TryGetInfo([NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        // ADT-Tweak-Start
        if (!_legacyBridge)
        {
            sponsor = null;
            return false;
        }
        // ADT-Tweak-End

        sponsor = _info;
        return _info != null;
    }
}
