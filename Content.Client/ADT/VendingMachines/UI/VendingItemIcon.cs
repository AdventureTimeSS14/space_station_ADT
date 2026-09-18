using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.VendingMachines.UI;

public sealed class VendingItemIcon : Control
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private readonly List<IconLayer> _layers = [];
    private Vector2 _contentSize;

    private struct IconLayer
    {
        public Texture Texture;
        public Vector2 Offset;
        public Vector2 Scale;
        public Color Color;
    }

    public VendingItemIcon()
    {
        IoCManager.InjectDependencies(this);
    }

    public void SetPrototype(EntityPrototype proto)
    {
        _layers.Clear();
        _contentSize = Vector2.Zero;

        if (proto.TryGetComponent<SpriteComponent>(out var sprite, _componentFactory))
        {
            var baseRsi = sprite.BaseRSI;
            foreach (var protoLayer in sprite.AllLayers)
            {
                if (protoLayer is not SpriteComponent.Layer layer || !layer.Visible || layer.Blank ||
                    layer.CopyToShaderParameters != null)
                {
                    continue;
                }

                Texture? texture = null;
                var rsi = layer.RSI ?? baseRsi;
                if (rsi != null && layer.State.IsValid && rsi.TryGetState(layer.State, out var state))
                {
                    texture = state.GetFrame(RsiDirection.South, 0);
                }
                else if (layer.Texture != null)
                {
                    texture = layer.Texture;
                }

                if (texture == null)
                    continue;

                var size = texture.Size * layer.Scale;
                var offset = layer.Offset * EyeManager.PixelsPerMeter;
                _contentSize = Vector2.Max(_contentSize, size + Vector2.Abs(offset) * 2);
                _layers.Add(new IconLayer
                {
                    Texture = texture,
                    Offset = offset,
                    Scale = layer.Scale,
                    Color = layer.Color,
                });
            }
        }

        InvalidateMeasure();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return _contentSize;
    }

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (_layers.Count == 0)
            return;

        var handle = renderHandle.DrawingHandleScreen;
        var center = Size / 2;
        var stretch = Size / _contentSize;

        foreach (var layer in _layers)
        {
            var size = layer.Texture.Size * layer.Scale * stretch;
            var rect = UIBox2.FromDimensions(center + layer.Offset * stretch - size / 2, size);
            handle.DrawTextureRect(layer.Texture, rect, layer.Color);
        }
    }
}
