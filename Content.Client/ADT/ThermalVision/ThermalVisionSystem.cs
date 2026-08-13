using Content.Shared.ADT.ThermalVision;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

public sealed class ThermalVisionSystem : SharedThermalVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private ThermalVisionOverlay? _overlay;
    private ThermalVisionEntityHighlightOverlay _throughWallsOverlay = default!;
    private EntityUid? _effect;
    private bool _active;
    private bool _activeAlt;

    private const string ScreenShaderId = "ADTThermalVisionScreenShader";
    private const string ScreenShaderAltId = "ADTThermalVisionScreenShaderHalfAlpha";
    private const string BodyShaderId = "ADTThermalBodyShader";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnDetached);

        _throughWallsOverlay = new(_prototypes.Index<ShaderPrototype>(BodyShaderId));
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
        if (_active && _activeAlt != ent.Comp.UseAlternativeShader)
            AttemptRemoveVision(force: true);

        if (_active)
            return;

        var shaderId = ent.Comp.UseAlternativeShader ? ScreenShaderAltId : ScreenShaderId;
        if (!_prototypes.TryIndex<ShaderPrototype>(shaderId, out var screenShader))
            return;

        _active = true;
        _activeAlt = ent.Comp.UseAlternativeShader;

        _overlay = new ThermalVisionOverlay(screenShader);
        _overlayMan.AddOverlay(_overlay);
        _overlayMan.AddOverlay(_throughWallsOverlay);

        _effect = SpawnAttachedTo(ent.Comp.EffectPrototype, Transform(ent).Coordinates);
        _xform.SetParent(_effect.Value, ent.Owner);
    }

    private void AttemptRemoveVision(bool force = false)
    {
        if (_player.LocalEntity == null && !force)
            return;

        if (!_active && _effect == null)
            return;

        _active = false;
        _activeAlt = false;

        if (_overlay != null)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay = null;
        }

        _overlayMan.RemoveOverlay(_throughWallsOverlay);

        if (_effect != null)
        {
            Del(_effect.Value);
            _effect = null;
        }
    }
}
