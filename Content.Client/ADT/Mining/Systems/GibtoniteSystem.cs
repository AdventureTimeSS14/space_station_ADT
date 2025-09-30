using Content.Shared.ADT.Mining.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Mining.Systems;

public sealed class GibtoniteSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GibtoniteComponent, AppearanceChangeEvent>(UpdateAppearanceSprite);
        SubscribeLocalEvent<GibtoniteComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GibtoniteComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(GibtoniteVisuals.State, out var stateLayer))
            sprite.LayerSetState(stateLayer, "gibtonite");

        if (sprite.LayerMapTryGet(GibtoniteVisuals.Active, out var activeLayer))
            sprite.LayerSetVisible(activeLayer, false);
    }

    private void UpdateAppearanceSprite(EntityUid uid, GibtoniteComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (args.AppearanceData.TryGetValue(GibtoniteVisuals.Active, out var activeObj) && activeObj is bool isActive)
        {
            if (sprite.LayerMapTryGet(GibtoniteVisuals.Active, out var activeLayer))
                sprite.LayerSetVisible(activeLayer, isActive);
        }

        if (args.AppearanceData.TryGetValue(GibtoniteVisuals.State, out var stateObj) && stateObj is GibtoniteState state)
        {
            if (sprite.LayerMapTryGet(GibtoniteVisuals.State, out var stateLayer))
            {
                string stateName = state switch
                {
                    GibtoniteState.Normal => "normal",
                    GibtoniteState.OhFuck => "ohfuck",
                    GibtoniteState.Nothing => "gibtonite",
                    _ => "gibtonite"
                };

                sprite.LayerSetState(stateLayer, stateName);
                sprite.LayerSetVisible(stateLayer, true);
            }
        }
    }
}