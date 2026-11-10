using System.Numerics;
using Content.Client.Clickable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTSpriteCompositionSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IClickMapManager _clickMap = default!;

    private const int ClickMapRadius = 2;

    private readonly Dictionary<EntityUid, List<string>> _keys = new();
    private readonly Dictionary<(string, string), Box2?> _stateBounds = new();

    public bool TryGetVisualBounds(SpriteComponent source, out Box2 bounds)
    {
        bounds = default;
        var found = false;

        foreach (var spriteLayer in source.AllLayers)
        {
            if (spriteLayer is not SpriteComponent.Layer layer)
                continue;

            if (!layer.Visible || layer.Blank || layer.CopyToShaderParameters != null)
                continue;

            var rsi = layer.ActualRsi;
            if (rsi == null || layer.State.Name == null)
                continue;

            if (GetStateBounds(rsi, layer.State) is not { } box)
                continue;

            var layerBox = new Box2(box.BottomLeft * layer.Scale + layer.Offset, box.TopRight * layer.Scale + layer.Offset);
            bounds = found ? bounds.Union(layerBox) : layerBox;
            found = true;
        }

        return found;
    }

    private Box2? GetStateBounds(RSI rsi, RSI.StateId state)
    {
        var key = (rsi.Path.ToString(), state.Name!);
        if (_stateBounds.TryGetValue(key, out var cached))
            return cached;

        var size = rsi.Size;
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = 0; y < size.Y; y++)
        {
            for (var x = 0; x < size.X; x++)
            {
                if (!_clickMap.IsOccluding(rsi, state, RsiDirection.South, 0, new Vector2i(x, y)))
                    continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        Box2? result = null;
        if (minX <= maxX)
        {
            if (maxX - minX >= ClickMapRadius * 2)
            {
                minX += ClickMapRadius;
                maxX -= ClickMapRadius;
            }

            if (maxY - minY >= ClickMapRadius * 2)
            {
                minY += ClickMapRadius;
                maxY -= ClickMapRadius;
            }

            const float ppm = 32f;
            var halfX = size.X / 2f;
            var halfY = size.Y / 2f;
            result = new Box2(
                (minX - halfX) / ppm,
                (halfY - (maxY + 1)) / ppm,
                (maxX + 1 - halfX) / ppm,
                (halfY - minY) / ppm);
        }

        _stateBounds[key] = result;
        return result;
    }

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
        ShaderInstance? shader = null,
        Color? fill = null,
        bool keepOwnShaders = false,
        ProtoId<ShaderPrototype>? replaceOwnShader = null)
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
            if (fill != null)
                color = fill.Value.WithAlpha(fill.Value.A * color.A);

            if (tint != null)
                color *= tint.Value;

            var layerOffset = Vector2.Transform(layer.Offset * scale, Matrix3x2.CreateRotation((float) rotation.Theta));

            _sprite.LayerSetColor(target.AsNullable(), newIndex, color);
            _sprite.LayerSetScale(target.AsNullable(), newIndex, layer.Scale * scale);
            _sprite.LayerSetOffset(target.AsNullable(), newIndex, offset + layerOffset);
            _sprite.LayerSetRotation(target.AsNullable(), newIndex, layer.Rotation + rotation);

            var keepOwn = keepOwnShaders
                && layer.Shader != null
                && !(replaceOwnShader != null && layer.ShaderPrototype == replaceOwnShader);

            if (shader != null && !keepOwn)
                target.Comp.LayerSetShader(newIndex, shader);
            else if (layer.Shader != null)
                target.Comp.LayerSetShader(newIndex, layer.Shader, layer.ShaderPrototype?.Id);

            keys.Add(key);

            if (index != null)
                index = newIndex + 1;
        }

        return index;
    }
}
