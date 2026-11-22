using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Cuffs;

public sealed partial class LegCuffVisualizerSystem : VisualizerSystem<LegCuffedComponent>
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegCuffedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LegCuffedComponent, ComponentShutdown>(OnShutdown);
    }
    protected override void OnAppearanceChange(EntityUid uid, LegCuffedComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, LegCuffVisuals.Applied, out var applied, args.Component))
            return;

        Entity<SpriteComponent?> ent = (uid, args.Sprite);

    }

    private void OnInit(EntityUid uid, LegCuffedComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), LegCuffVisualLayers.Overlay);
        SpriteSystem.LayerSetSprite((uid, sprite), LegCuffVisualLayers.Overlay, component.CuffedSprite);
        SpriteSystem.LayerSetVisible((uid, sprite), LegCuffVisualLayers.Overlay, true);
    }

    private void OnShutdown(EntityUid uid, LegCuffedComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), LegCuffVisualLayers.Overlay, out var layer, false))
            return;

        SpriteSystem.RemoveLayer((uid, sprite), layer);
    }
}
