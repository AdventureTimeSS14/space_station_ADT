using Content.Shared.ADT.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.ADT.Combat;

public sealed partial class HeldItemShowModeSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_cfg.GetCVar(CCVars.HudHeldItemShow))
            return;

        if (_cfg.GetCVar(ADTCCVars.HeldItemShowMode) == (int) HeldItemShowMode.Always)
            _cfg.SetCVar(ADTCCVars.HeldItemShowMode, (int) HeldItemShowMode.Never);

        _cfg.SetCVar(CCVars.HudHeldItemShow, true);
        _cfg.SaveToFile();
    }
}
