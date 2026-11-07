using Content.Shared.ADT.Shields;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Shields;

public sealed class ShieldGeneratorVisualizerSystem : VisualizerSystem<ShieldGeneratorComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ShieldGeneratorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var running = false;
        var capNorth = false;
        var capEast = false;
        var capWest = false;

        if (args.Component != null)
        {
            AppearanceSystem.TryGetData<bool>(uid, ShieldGeneratorVisuals.Running, out running, args.Component);
            AppearanceSystem.TryGetData<bool>(uid, ShieldGeneratorVisuals.CapacitorNorth, out capNorth, args.Component);
            AppearanceSystem.TryGetData<bool>(uid, ShieldGeneratorVisuals.CapacitorEast, out capEast, args.Component);
            AppearanceSystem.TryGetData<bool>(uid, ShieldGeneratorVisuals.CapacitorWest, out capWest, args.Component);
        }

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), ShieldGeneratorVisualLayers.Base, running ? "generator1" : "generator0");

        if (args.Sprite.LayerExists(ShieldGeneratorVisualLayers.CapacitorNorth, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldGeneratorVisualLayers.CapacitorNorth, capNorth);
        if (args.Sprite.LayerExists(ShieldGeneratorVisualLayers.CapacitorEast, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldGeneratorVisualLayers.CapacitorEast, capEast);
        if (args.Sprite.LayerExists(ShieldGeneratorVisualLayers.CapacitorWest, false))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldGeneratorVisualLayers.CapacitorWest, capWest);
    }
}

public sealed class ShieldSegmentVisualizerSystem : VisualizerSystem<ShieldSegmentComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ShieldSegmentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var active = true;
        var overcharged = false;
        var floor = false;

        if (args.Component != null)
        {
            AppearanceSystem.TryGetData<bool>(uid, ShieldSegmentVisuals.Active, out active, args.Component);
            AppearanceSystem.TryGetData<bool>(uid, ShieldSegmentVisuals.Overcharged, out overcharged, args.Component);
            AppearanceSystem.TryGetData<bool>(uid, ShieldSegmentVisuals.Floor, out floor, args.Component);
        }

        var state = !active
            ? "shield_broken"
            : floor
                ? "shield"
                : overcharged
                    ? "shield_overcharged"
                    : "shield_normal";

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), ShieldSegmentVisualLayers.Base, state);
        SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldSegmentVisualLayers.Base, true);
    }
}

public sealed class ShieldConduitVisualizerSystem : VisualizerSystem<ShieldConduitComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ShieldConduitComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var connected = false;
        if (args.Component != null)
            AppearanceSystem.TryGetData<bool>(uid, ShieldConduitVisuals.Connected, out connected, args.Component);

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), ShieldConduitVisualLayers.Base, connected ? "conduit_1" : "conduit_0");
        SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldConduitVisualLayers.Base, true);
    }
}

public sealed class ShieldDiffuserVisualizerSystem : VisualizerSystem<ShieldDiffuserComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ShieldDiffuserComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var state = ShieldDiffuserState.Off;
        if (args.Component != null)
            AppearanceSystem.TryGetData<ShieldDiffuserState>(uid, ShieldDiffuserVisuals.State, out state, args.Component);

        var iconState = state switch
        {
            ShieldDiffuserState.On => "fdiffuser_on",
            ShieldDiffuserState.Emergency => "fdiffuser_emergency",
            _ => "fdiffuser_off",
        };

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), ShieldDiffuserVisualLayers.Base, iconState);
        SpriteSystem.LayerSetVisible((uid, args.Sprite), ShieldDiffuserVisualLayers.Base, true);
    }
}

public sealed class HandheldShieldDiffuserVisualizerSystem : VisualizerSystem<HandheldShieldDiffuserComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HandheldShieldDiffuserComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var enabled = false;
        if (args.Component != null)
            AppearanceSystem.TryGetData<bool>(uid, HandheldShieldDiffuserVisuals.Enabled, out enabled, args.Component);

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), HandheldShieldDiffuserVisualLayers.Base, enabled ? "hdiffuser_on" : "hdiffuser_off");
        SpriteSystem.LayerSetVisible((uid, args.Sprite), HandheldShieldDiffuserVisualLayers.Base, true);
    }
}