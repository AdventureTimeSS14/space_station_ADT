using Content.Client.DamageState;
using Content.Shared._VG.Xenobiology;
using Content.Shared._VG.Xenobiology.Components;
using Content.Shared.Mobs;
using Robust.Client.GameObjects;

namespace Content.Client._VG.Xenobiology;

/// <summary>
/// This handles visual changes in mobs which can transition growth states.
/// </summary>
public sealed class MobGrowthVisualizerSystem : VisualizerSystem<MobGrowthComponent>
{
    //I have a feeling this may need some protective functions.
    protected override void OnAppearanceChange(EntityUid uid, MobGrowthComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<string>(uid, GrowthStateVisuals.Sprite, out var rsi, args.Component))
            return;

        args.Sprite.LayerSetRSI(DamageStateVisualLayers.Base, rsi);
    }
}
