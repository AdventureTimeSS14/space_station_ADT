using Content.Client.ADT.Supermatter.Components;
using Content.Shared.ADT.Supermatter.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Supermatter.Systems;

public sealed class SupermatterVisualizerSystem : VisualizerSystem<SupermatterVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SupermatterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var crystalLayer = args.Sprite.LayerMapGet(SupermatterVisuals.Crystal);

        if (AppearanceSystem.TryGetData(uid, SupermatterVisuals.Crystal, out SupermatterCrystalState crystalState, args.Component) &&
            component.CrystalVisuals.TryGetValue(crystalState, out var crystalData))
        {
            args.Sprite.LayerSetState(crystalLayer, crystalData.State);
        }
    }
}
