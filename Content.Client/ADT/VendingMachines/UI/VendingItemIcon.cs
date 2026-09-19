using System.Numerics;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Rounding;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

    public void SetPrototype(EntityPrototype proto, ReturnedItemDisplay? returned = null)
    {
        _layers.Clear();
        _contentSize = Vector2.Zero;

        if (!proto.TryGetComponent<SpriteComponent>(out var sprite, _componentFactory))
        {
            InvalidateMeasure();
            return;
        }

        var fillInfo = GetFillInfo(proto, sprite, returned);
        var fillLayer = fillInfo?.Layer;
        var fillState = fillInfo?.State;
        var fillColor = fillInfo?.Color;
        var baseRsi = sprite.BaseRSI;

        foreach (var protoLayer in sprite.AllLayers)
        {
            if (protoLayer is not SpriteComponent.Layer layer || layer.Blank ||
                layer.CopyToShaderParameters != null)
            {
                continue;
            }

            var isFillLayer = layer == fillLayer;
            if (!layer.Visible && !isFillLayer)
                continue;

            Texture? texture = null;
            var rsi = layer.RSI ?? baseRsi;
            var state = isFillLayer && fillState != null ? (RSI.StateId) fillState : layer.State;
            if (rsi != null && state.IsValid && rsi.TryGetState(state, out var stateData))
            {
                texture = stateData.GetFrame(RsiDirection.South, 0);
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
                Color = isFillLayer && fillColor != null ? fillColor.Value : layer.Color,
            });
        }

        InvalidateMeasure();
    }

    private (SpriteComponent.Layer Layer, string State, Color? Color)? GetFillInfo(EntityPrototype proto, SpriteComponent sprite,
        ReturnedItemDisplay? returned = null)
    {
        if (!proto.TryGetComponent<SolutionContainerVisualsComponent>(out var visuals, _componentFactory)
            || visuals.Metamorphic
            || visuals.MaxFillLevels <= 0
            || string.IsNullOrEmpty(visuals.FillBaseName))
        {
            return null;
        }

        float fillFraction;
        Color? fillColor;
        if (returned != null)
        {
            fillFraction = returned.FillFraction;
            fillColor = returned.FillColor;
        }
        else
        {
            if (!proto.TryGetComponent<SolutionContainerManagerComponent>(out var container, _componentFactory))
                return null;

            var solutions = container.Solutions;
            if (solutions == null || solutions.Count == 0)
                return null;

            Solution? solution = null;
            foreach (var pair in solutions)
            {
                if (visuals.SolutionName == null || pair.Key == visuals.SolutionName)
                {
                    solution = pair.Value;
                    break;
                }
            }

            if (solution == null || solution.Volume <= FixedPoint2.Zero || solution.MaxVolume <= FixedPoint2.Zero)
                return null;

            fillFraction = solution.FillFraction;
            fillColor = visuals.ChangeColor ? solution.GetColor(_prototypeManager) : null;
        }

        if (fillFraction <= 0)
            return null;

        var level = ContentHelpers.RoundToLevels(fillFraction, 1, visuals.MaxFillLevels + 1);
        if (level <= 0)
            return null;

        SpriteComponent.Layer? fillLayer;
        try
        {
            fillLayer = sprite[visuals.Layer] as SpriteComponent.Layer;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        if (fillLayer == null)
            return null;

        return (fillLayer, visuals.FillBaseName + level, visuals.ChangeColor ? fillColor : null);
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
