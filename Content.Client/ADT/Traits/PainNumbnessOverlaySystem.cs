using Content.Shared.ADT.Traits.Assorted;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Content.Client.UserInterface.Systems.DamageOverlays.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.ADT.Traits;

/// <summary>
/// Client-side system that hides the pain overlay (red screen effect) when PainNumbness component is active
/// and the player is in Alive state. In Critical or Dead state, overlays are shown normally.
/// </summary>
public sealed class PainNumbnessOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private DamageOverlay? _damageOverlay;
    private bool _hasPainNumbness;
    private MobState _currentMobState;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdatePainNumbnessState();
        UpdateMobState(args.Entity);
        UpdatePainOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateMobState(args.Target);
        UpdatePainOverlay();
    }

    private void UpdatePainNumbnessState()
    {
        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        _hasPainNumbness = HasComp<PainNumbnessStatusEffectComponent>(localEntity.Value);
    }

    private void UpdateMobState(EntityUid entity)
    {
        if (TryComp<MobStateComponent>(entity, out var mobState))
            _currentMobState = mobState.CurrentState;
    }

    private void UpdatePainOverlay()
    {
        if (_damageOverlay == null)
        {
            if (!_overlayManager.TryGetOverlay(out DamageOverlay? overlay))
                return;

            _damageOverlay = overlay;
        }

        if (_hasPainNumbness && _currentMobState == MobState.Alive)
        {
            _damageOverlay.PainLevel = 0f;
        }
    }
}
