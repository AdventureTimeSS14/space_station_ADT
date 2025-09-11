using Content.Client.ADT.Geras.Component;
using Content.Shared.ADT.Geras;
using Robust.Client.GameObjects;
using Content.Shared.Item;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.ADT.Geras;

public sealed class GerasSystem : VisualizerSystem<GerasComponent>
{

    protected override void OnAppearanceChange(EntityUid uid, GerasComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!AppearanceSystem.TryGetData(uid, GeraColor.Color, out Color color, args.Component))
            return;

        foreach (var spriteLayer in args.Sprite.AllLayers)
        {
            sprite.Color = color;
        }
    }
}
