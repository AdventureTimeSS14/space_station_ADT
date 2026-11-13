using Content.Shared.ADT.Crushers.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.Crushers;

public sealed class CrusherRageOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private CrusherRageOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCrusherRageOverlayStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<ADTCrusherRageOverlayStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<ADTCrusherRageOverlayStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<ADTCrusherRageOverlayStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new CrusherRageOverlay();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnApplied(Entity<ADTCrusherRageOverlayStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        Show(ent.Comp);
    }

    private void OnRemoved(Entity<ADTCrusherRageOverlayStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<ADTCrusherRageOverlayStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        Show(ent.Comp);
    }

    private void OnPlayerDetached(Entity<ADTCrusherRageOverlayStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void Show(ADTCrusherRageOverlayStatusEffectComponent comp)
    {
        _overlay.Configure(comp);

        if (!_overlayMan.HasOverlay<CrusherRageOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }
}
