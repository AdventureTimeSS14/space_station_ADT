using Content.Client.ADT.RoleIntro;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasRulesSystem : EntitySystem
{
    private const int RulesVersion = 1;

    private static readonly TimeSpan LockTime = TimeSpan.FromSeconds(15);

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ADTRoleIntroWindow? _window;

    public override void Shutdown()
    {
        base.Shutdown();

        _window?.Orphan();
        _window = null;
    }

    public void ShowIfNeeded()
    {
        if (_window != null || _cfg.GetCVar(ADTCCVars.CanvasRulesAccepted) >= RulesVersion)
            return;

        _window = new ADTRoleIntroWindow(
            Loc.GetString("adt-canvas-rules-title"),
            Loc.GetString("adt-canvas-rules-text"),
            LockTime,
            true);

        _window.OnClose += OnClosed;
        _window.OpenCentered();
    }

    private void OnClosed()
    {
        _window = null;
        _cfg.SetCVar(ADTCCVars.CanvasRulesAccepted, RulesVersion);
        _cfg.SaveToFile();
    }
}
