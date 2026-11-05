using System.Numerics;
using Content.Client.DisplacementMap;
using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTSpriteCompositionSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    private readonly Dictionary<EntityUid, List<string>> _keys = new();

    public void Clear(Entity<SpriteComponent> target)
    {
        if (!_keys.TryGetValue(target, out var keys))
            return;

        for (var i = keys.Count - 1; i >= 0; i--)
        {
            _sprite.RemoveLayer(target.AsNullable(), keys[i], false);
        }

        keys.Clear();
    }

    public void Forget(EntityUid uid)
    {
        _keys.Remove(uid);
    }

    private List<string> GetKeys(EntityUid uid)
    {
        if (!_keys.TryGetValue(uid, out var keys))
        {
            keys = new List<string>();
            _keys[uid] = keys;
        }

        return keys;
    }

    public int AddLayer(Entity<SpriteComponent> target, string key, SpriteSpecifier specifier, Vector2 offset, int? index = null)
    {
        var newIndex = _sprite.AddLayer(target.AsNullable(), specifier, index);
        _sprite.LayerMapSet(target.AsNullable(), key, newIndex);
        _sprite.LayerSetOffset(target.AsNullable(), newIndex, offset);
        GetKeys(target).Add(key);
        return newIndex;
    }

    public int? CopyLayers(
        Entity<SpriteComponent> target,
        SpriteComponent source,
        string prefix,
        Vector2 offset,
        Vector2 scale,
        Angle rotation,
        int? index,
        Color? tint = null,
        DisplacementData? displacement = null,
        ShaderInstance? shader = null)
    {
        var keys = GetKeys(target);
        var counter = 0;

        foreach (var spriteLayer in source.AllLayers)
        {
            if (spriteLayer is not SpriteComponent.Layer layer)
                continue;

            if (!layer.Visible || layer.Blank || layer.CopyToShaderParameters != null)
                continue;

            var rsi = layer.ActualRsi;
            if (rsi == null || layer.State.Name == null)
                continue;

            var key = $"{prefix}-{counter++}";
            var newIndex = _sprite.AddLayer(target.AsNullable(), new SpriteSpecifier.Rsi(rsi.Path, layer.State.Name), index);
            _sprite.LayerMapSet(target.AsNullable(), key, newIndex);

            var color = layer.Color * source.Color;
            if (tint != null)
                color = tint.Value.WithAlpha(tint.Value.A * color.A);

            var layerOffset = Vector2.Transform(layer.Offset * scale, Matrix3x2.CreateRotation((float) rotation.Theta));

            _sprite.LayerSetColor(target.AsNullable(), newIndex, color);
            _sprite.LayerSetScale(target.AsNullable(), newIndex, layer.Scale * scale);
            _sprite.LayerSetOffset(target.AsNullable(), newIndex, offset + layerOffset);
            _sprite.LayerSetRotation(target.AsNullable(), newIndex, layer.Rotation + rotation);

            if (shader != null)
                target.Comp.LayerSetShader(newIndex, shader);
            else if (layer.Shader != null)
                target.Comp.LayerSetShader(newIndex, layer.Shader, layer.ShaderPrototype?.Id);

            keys.Add(key);

            if (displacement != null && _displacement.TryAddDisplacement(displacement, target, newIndex, key, out var displacementKey))
            {
                keys.Add(displacementKey);
                newIndex++;
            }

            if (index != null)
                index = newIndex + 1;
        }

        return index;
    }
}
