using Content.Shared.ADT.Holomap;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Holomap;

public sealed class HolomapVisualizerSystem : VisualizerSystem<HolomapComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HolomapComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var powered = false;
        if (TryComp(uid, out TransformComponent? xform) && xform.Anchored)
        {
            if (AppearanceSystem.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var isPowered, args.Component))
            {
                powered = isPowered;
            }
        }

        var mode = HolomapMode.Battlemap;
        if (AppearanceSystem.TryGetData<HolomapMode>(uid, HolomapVisuals.Mode, out var currentMode, args.Component))
        {
            mode = currentMode;
        }

        UpdateLayers((uid, args.Sprite), powered, mode);
    }

    private void UpdateLayers(Entity<SpriteComponent?> sprite, bool powered, HolomapMode mode)
    {
        SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Base, true);
        SpriteSystem.LayerSetRsiState(sprite, HolomapVisualLayers.Base, "base");

        if (!powered)
        {
            SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Unshaded, false);
            SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Battlemap, false);
            SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.BattlemapText, false);
            SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Lavaland, false);
            return;
        }

        SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Unshaded, true);
        SpriteSystem.LayerSetRsiState(sprite, HolomapVisualLayers.Unshaded, "unshaded");
        SpriteSystem.LayerSetAutoAnimated(sprite, HolomapVisualLayers.Unshaded, true);

        SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Battlemap, false);
        SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.BattlemapText, false);
        SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Lavaland, false);

        switch (mode)
        {
            case HolomapMode.Battlemap:
                SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Battlemap, true);
                SpriteSystem.LayerSetRsiState(sprite, HolomapVisualLayers.Battlemap, "battlemap-unshaded");
                SpriteSystem.LayerSetAutoAnimated(sprite, HolomapVisualLayers.Battlemap, true);

                SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.BattlemapText, true);
                SpriteSystem.LayerSetRsiState(sprite, HolomapVisualLayers.BattlemapText, "battlemap_text-unshaded");
                SpriteSystem.LayerSetAutoAnimated(sprite, HolomapVisualLayers.BattlemapText, true);
                break;

            case HolomapMode.Lavaland:
                SpriteSystem.LayerSetVisible(sprite, HolomapVisualLayers.Lavaland, true);
                SpriteSystem.LayerSetRsiState(sprite, HolomapVisualLayers.Lavaland, "lavaland");
                SpriteSystem.LayerSetAutoAnimated(sprite, HolomapVisualLayers.Lavaland, true);
                break;
        }
    }
}

public enum HolomapVisualLayers : byte
{
    Base,
    Unshaded,
    Battlemap,
    BattlemapText,
    Lavaland
}
