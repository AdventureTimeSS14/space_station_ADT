using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Wieldable;
using Robust.Shared.Player;
using Robust.Shared.Configuration;

namespace Content.Client.ADT.Wieldable;

public sealed partial class WieldOrderClientSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ADTCCVars.WieldBeforeRack, OnChanged);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnChanged(bool wieldBeforeRack)
        => RaiseNetworkEvent(new SetWieldOrderEvent(wieldBeforeRack));

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
        => OnChanged(_cfg.GetCVar(ADTCCVars.WieldBeforeRack));
}
