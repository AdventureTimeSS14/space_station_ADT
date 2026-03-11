using Content.Shared.ADT.Mangal;
using Content.Shared.Atmos;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Mangal;

public sealed class MangalVisualizerSystem : VisualizerSystem<MangalVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MangalVisualizerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, MangalVisualizerComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        UpdateVisuals(uid, sprite);
    }

    protected override void OnAppearanceChange(EntityUid uid, MangalVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateVisuals(uid, args.Sprite);
    }

    private void UpdateVisuals(EntityUid uid, SpriteComponent sprite)
    {
        var onFire = false;
        AppearanceSystem.TryGetData(uid, FireVisuals.OnFire, out onFire);

        var fireStacks = 0f;
        AppearanceSystem.TryGetData(uid, FireVisuals.FireStacks, out fireStacks);

        SpriteSystem.LayerSetRsiState((uid, sprite), MangalVisualLayers.Main, "mangal");

        var hasFuel = fireStacks > 0.01f;

        SpriteSystem.LayerSetVisible((uid, sprite), MangalVisualLayers.Coals, hasFuel && !onFire);

        SpriteSystem.LayerSetVisible((uid, sprite), MangalVisualLayers.Fire, onFire);
    }
}
