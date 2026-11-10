using System.Numerics;
using Content.Shared.ADT.Cooking.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private static readonly ProtoId<ShaderPrototype> SauceShader = "ADTFoodSauce";

    private ShaderInstance? CreateSauceShader(EntityUid dish)
    {
        if (!TryComp<ADTDishComponent>(dish, out var comp) || comp.Glaze == null && comp.Spice == null)
            return null;

        var shader = _proto.Index(SauceShader).InstanceUnique();
        shader.SetParameter("glazeColor", ToParameter(comp.Glaze));
        shader.SetParameter("spiceColor", ToParameter(comp.Spice));
        shader.SetParameter("seed", (dish.Id % 89) * 0.37f);
        return shader;
    }

    private static Vector4 ToParameter(Color? color)
    {
        return color is { } c ? new Vector4(c.R, c.G, c.B, c.A) : Vector4.Zero;
    }

    private void ApplySauce(Entity<SpriteComponent> ent)
    {
        var shader = CreateSauceShader(ent);
        var index = -1;
        foreach (var spriteLayer in ent.Comp.AllLayers)
        {
            index++;
            if (spriteLayer is not SpriteComponent.Layer layer || layer.CopyToShaderParameters != null)
                continue;

            var ours = layer.ShaderPrototype == SauceShader;
            if (shader != null && (layer.ShaderPrototype == null || ours))
                ent.Comp.LayerSetShader(index, shader, SauceShader.Id);
            else if (shader == null && ours)
                ent.Comp.LayerSetShader(index, null, null);
        }
    }
}
