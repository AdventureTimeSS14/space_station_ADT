using Content.Client.Ensnaring; // EnsnaredVisualLayers
using Content.Shared.Cuffs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Cuffs;

public sealed partial class LegCuffVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegCuffedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegCuffedComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<LegCuffedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegCuffedComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnStartup(EntityUid uid, LegCuffedComponent comp, ComponentStartup args)
    {
        Apply(uid, comp);
    }

    private void OnAppearanceChange(EntityUid uid, LegCuffedComponent comp, ref AppearanceChangeEvent args)
    {
        Apply(uid, comp);
    }

    private void OnRemove(EntityUid uid, LegCuffedComponent comp, ComponentRemove args)
    {
        Clear(uid);
    }

    private void OnShutdown(EntityUid uid, LegCuffedComponent comp, ComponentShutdown args)
    {
        Clear(uid);
    }

    private void Apply(EntityUid uid, LegCuffedComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        Entity<SpriteComponent?> ent = (uid, sprite);

        var layer = _sprite.LayerMapReserve(ent, LegCuffLayer.Overlay);
        _sprite.LayerSetSprite(ent, layer,
            new SpriteSpecifier.Rsi(new ResPath(comp.CuffedRSI), comp.BodyIconState));
        _sprite.LayerSetVisible(ent, layer, true);

        if (_sprite.LayerMapTryGet(ent, EnsnaredVisualLayers.Ensnared, out var ensLayer, false))
            _sprite.LayerSetVisible(ent, ensLayer, false);
    }

    private void Clear(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        Entity<SpriteComponent?> ent = (uid, sprite);

        if (_sprite.LayerMapTryGet(ent, LegCuffLayer.Overlay, out var layer, false))
            _sprite.LayerSetVisible(ent, layer, false);
    }
}

public enum LegCuffLayer : byte
{
    Overlay
}
