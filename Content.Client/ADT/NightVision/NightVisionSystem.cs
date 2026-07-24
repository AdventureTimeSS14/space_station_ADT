using Content.Shared.ADT.NightVision;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.NightVision;

/// <summary>
/// Client night vision via Starlight-style screen shader + attached light effect.
/// Hooks into existing ADT <see cref="SharedNightVisionSystem"/> state (Off/Full).
/// </summary>
public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private NightVisionOverlay? _overlay;
    private EntityUid? _effect;
    private string? _activeShader;
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
        // Recreate if shader or light effect differs (device ПНВ vs innate).
        var effectId = ent.Comp.EffectPrototype.Id;
        if (_overlay != null && (_activeShader != ent.Comp.Shader || _activeEffect != effectId))
            AttemptRemoveVision(force: true);

        if (_effect != null)
            return;

        if (!_prototypes.TryIndex<ShaderPrototype>(ent.Comp.Shader, out var shaderProto))
            return;

        _overlay = new NightVisionOverlay(shaderProto);
        _activeShader = ent.Comp.Shader;
        _activeEffect = effectId;
        _overlayMan.AddOverlay(_overlay);

        _effect = SpawnAttachedTo(ent.Comp.EffectPrototype, Transform(ent).Coordinates);
        _xform.SetParent(_effect.Value, ent.Owner);
    }

    private void AttemptRemoveVision(bool force = false)
    {
        if (_player.LocalEntity == null && !force)
            return;

        if (_overlay != null)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay = null;
            _activeShader = null;
            _activeEffect = null;
        }

        if (_effect != null)
        {
            Del(_effect.Value);
            _effect = null;
        }
    }
}
