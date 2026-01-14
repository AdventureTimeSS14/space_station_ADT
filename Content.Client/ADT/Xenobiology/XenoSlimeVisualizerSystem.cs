using System.Diagnostics;
using Content.Client.DamageState;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Xenobiology;

/// <summary>
/// This handles visual changes in slimes between breeds.
/// </summary>
public sealed class XenoSlimeVisualizerSystem : VisualizerSystem<SlimeComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void OnAppearanceChange(EntityUid uid, SlimeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<Color>(uid, XenoSlimeVisuals.Color, out var color, args.Component))
            return;

        foreach (var layer in args.Sprite.AllLayers)
            layer.Color = color.WithAlpha(layer.Color.A);

        if (AppearanceSystem.TryGetData<string>(uid, XenoSlimeVisuals.Shader, out var shader, args.Component))
        {
            var spriteComp = args.Sprite;
            var newShader = _proto.Index<ShaderPrototype>(shader).InstanceUnique();

            var layerExists = spriteComp.LayerMapTryGet(DamageStateVisualLayers.Base, out var layerKey);

            if (!layerExists)
                return;

            spriteComp.LayerSetShader(layerKey, newShader);
            spriteComp.GetScreenTexture = newShader is not null;
            spriteComp.RaiseShaderEvent = newShader is not null;
        }
    }
}
