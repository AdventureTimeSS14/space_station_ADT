using Content.Shared.ADT.ThermalVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

/// <summary>
/// Client thermal vision: screen LUT + gas heat only.
/// No through-walls brightness pass and no personal PointLight fill.
/// </summary>
public sealed class ThermalVisionSystem : SharedThermalVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private ThermalVisionOverlay _overlay = default!;
    private ThermalVisionOverlay _altOverlay = default!;
    private GasTileThermalVisionOverlay _gasOverlay = default!;
    private bool _active;

    private const string ThermalShaderId = "ADTThermalVisionScreenShader";
    private const string ThermalAltShaderId = "ADTThermalVisionScreenShaderHalfAlpha";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnDetached);

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
        if (_active)
            return;

        _active = true;
        _overlayMan.AddOverlay(_gasOverlay);
        if (ent.Comp.UseAlternativeShader)
            _overlayMan.AddOverlay(_altOverlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }

    private void AttemptRemoveVision(bool force = false)
    {
        if (_player.LocalEntity == null && !force)
            return;

        if (!_active)
            return;

        _active = false;
        _overlayMan.RemoveOverlay(_gasOverlay);
        _overlayMan.RemoveOverlay(_overlay);
        _overlayMan.RemoveOverlay(_altOverlay);
    }
}
