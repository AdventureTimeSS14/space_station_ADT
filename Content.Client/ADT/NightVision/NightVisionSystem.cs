using Content.Shared.ADT.NightVision;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.NightVision;

/// <summary>
/// Client night vision via an attached light effect.
/// Hooks into existing ADT <see cref="SharedNightVisionSystem"/> state (Off/Full).
/// </summary>
public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private EntityUid? _effect;
    private string? _activeEffect;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
        => NightVisionChanged(ent);

    private void OnDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
        => AttemptRemoveVision(force: true);

    protected override void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        if (ent.Comp.State == NightVisionState.Full)
            AttemptAddVision(ent);
        else
            AttemptRemoveVision();
    }

    protected override void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        AttemptRemoveVision();
    }

    private void AttemptAddVision(Entity<NightVisionComponent> ent)
    {
        // Recreate if the light effect differs (device ПНВ vs innate).
        var effectId = ent.Comp.EffectPrototype.Id;
        if (_effect != null && _activeEffect != effectId)
            AttemptRemoveVision(force: true);

        if (_effect != null)
            return;

        _activeEffect = effectId;

        _effect = SpawnAttachedTo(ent.Comp.EffectPrototype, Transform(ent).Coordinates);
        _xform.SetParent(_effect.Value, ent.Owner);
    }

    private void AttemptRemoveVision(bool force = false)
    {
        if (_player.LocalEntity == null && !force)
            return;

        if (_effect != null)
        {
            Del(_effect.Value);
            _effect = null;
            _activeEffect = null;
        }
    }
}
