using Content.Shared.ADT.Burzhujka;
using Content.Shared.Atmos;
using Content.Shared.Nutrition.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.ADT.Burzhujka;

public sealed class BurzhujkaVisualizerSystem : VisualizerSystem<BurzhujkaVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurzhujkaVisualizerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BurzhujkaVisualizerComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnStartup(EntityUid uid, BurzhujkaVisualizerComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var opened = TryComp<OpenableComponent>(uid, out var openable) && openable.Opened;
        UpdateVisuals(uid, sprite, opened);
    }

    private void OnAfterHandleState(EntityUid uid, BurzhujkaVisualizerComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var opened = TryComp<OpenableComponent>(uid, out var openable) && openable.Opened;
        UpdateVisuals(uid, sprite, opened);
    }

    protected override void OnAppearanceChange(EntityUid uid, BurzhujkaVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var opened = false;
        if (TryComp<OpenableComponent>(uid, out var openable))
        {
            opened = openable.Opened;
        }

        UpdateVisuals(uid, args.Sprite, opened);
    }

    private void UpdateVisuals(EntityUid uid, SpriteComponent sprite, bool opened)
    {
        var onFire = false;
        AppearanceSystem.TryGetData(uid, FireVisuals.OnFire, out onFire);

        var fireStacks = 0f;
        AppearanceSystem.TryGetData(uid, FireVisuals.FireStacks, out fireStacks);

        var targetState = opened ? "open" : "furnace";
        SpriteSystem.LayerSetRsiState((uid, sprite), BurzhujkaVisualLayers.Main, targetState);

        var hasFuel = fireStacks > 0.01f;

        SpriteSystem.LayerSetVisible((uid, sprite), BurzhujkaVisualLayers.Coals, opened && hasFuel && !onFire);

        SpriteSystem.LayerSetVisible((uid, sprite), BurzhujkaVisualLayers.Fire, opened && onFire);
    }
}
