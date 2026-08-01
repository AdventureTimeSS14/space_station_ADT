using Content.Shared.ADT.ThermalVision;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

/// <summary>
/// Client thermal vision via Starlight-style screen + through-walls highlight overlays.
/// Hooks into existing ADT <see cref="SharedThermalVisionSystem"/> state (Off/Full).
/// </summary>
public sealed class ThermalVisionSystem : SharedThermalVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private ThermalVisionEntityHighlightOverlay _throughWallsOverlay = default!;
    private ThermalVisionOverlay _overlay = default!;
    private ThermalVisionOverlay _altOverlay = default!;
    private GasTileThermalVisionOverlay _gasOverlay = default!;
    private EntityUid? _effect;

    private const string ThermalShaderId = "ADTThermalVisionScreenShader";
    private const string ThermalAltShaderId = "ADTThermalVisionScreenShaderHalfAlpha";
    private const string BrightnessShaderId = "ADTBrightnessShader";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnDetached);

        _throughWallsOverlay = new(_prototypes.Index<ShaderPrototype>(BrightnessShaderId));
        _overlay = new(_prototypes.Index<ShaderPrototype>(ThermalShaderId));
        _altOverlay = new(_prototypes.Index<ShaderPrototype>(ThermalAltShaderId));
        _gasOverlay = new GasTileThermalVisionOverlay();
    }

    private void OnAttached(Entity<ThermalVisionComponent> ent, ref LocalPlayerAttachedEvent args)
        => ThermalVisionChanged(ent);

    private void OnDetached(Entity<ThermalVisionComponent> ent, ref LocalPlayerDetachedEvent args)
        => AttemptRemoveVision(force: true);

    protected override void ThermalVisionChanged(Entity<ThermalVisionComponent> ent)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        if (ent.Comp.State == ThermalVisionState.Full)
            AttemptAddVision(ent);
        else
            AttemptRemoveVision();
    }

    protected override void ThermalVisionRemoved(Entity<ThermalVisionComponent> ent)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        AttemptRemoveVision();
    }

    private void AttemptAddVision(Entity<ThermalVisionComponent> ent)
    {
        if (_effect != null)
            return;

        _overlayMan.AddOverlay(_gasOverlay);
        _overlayMan.AddOverlay(_throughWallsOverlay);
        if (ent.Comp.UseAlternativeShader)
            _overlayMan.AddOverlay(_altOverlay);
        else
            _overlayMan.AddOverlay(_overlay);

        _effect = SpawnAttachedTo(ent.Comp.EffectPrototype, Transform(ent).Coordinates);
        _xform.SetParent(_effect.Value, ent.Owner);
    }

    private void AttemptRemoveVision(bool force = false)
    {
        if (_player.LocalEntity == null && !force)
            return;

        _overlayMan.RemoveOverlay(_gasOverlay);
        _overlayMan.RemoveOverlay(_throughWallsOverlay);
        _overlayMan.RemoveOverlay(_overlay);
        _overlayMan.RemoveOverlay(_altOverlay);

        if (_effect != null)
        {
            Del(_effect.Value);
            _effect = null;
        }
    }
}
