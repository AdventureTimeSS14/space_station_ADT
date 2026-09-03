using Content.Shared.ADT.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.Overlays;

public sealed partial class ScreenWaveSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ScreenWaveOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScreenWaveComponent, StatusEffectAppliedEvent>(OnApplied);

        SubscribeLocalEvent<ScreenWaveComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<ScreenWaveComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnApplied(Entity<ScreenWaveComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.TimeTicker = 0;
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<ScreenWaveComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<ScreenWaveComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlay.Intoxication = 0;
        _overlay.TimeTicker = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
