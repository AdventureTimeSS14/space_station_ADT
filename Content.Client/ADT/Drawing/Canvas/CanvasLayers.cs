using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class CanvasLayer : IDisposable
{
    public readonly int Id;
    public readonly PaintSurface Surface;
    public readonly BrushStroker Stroker;

    public string Name;
    public bool Visible = true;
    public float Opacity = 1f;
    public bool Dirty;

    public CanvasLayer(int id, string name, int width, int height, BrushSettings brush)
    {
        Id = id;
        Name = name;
        Surface = new PaintSurface(width, height, Color.Transparent, sparse: true)
        {
            EraseToPaper = false,
            UndoBudget = 64L * 1024 * 1024,
        };
        Stroker = new BrushStroker(Surface, brush);
        Stroker.StrokeFinished += (_, _, _) => Dirty = true;
        Surface.HistoryChanged += () => Dirty = true;
    }

    public void Dispose()
    {
        Surface.Dispose();
    }
}

public sealed class CanvasLayerStack : IDisposable
{
    public const int MaxLayers = 8;

    private readonly List<CanvasLayer> _layers = new();
    private readonly int _width;
    private readonly int _height;
    private readonly BrushSettings _brush;
    private int _nextId = 1;

    public CanvasLayerStack(int width, int height, BrushSettings brush)
    {
        _width = width;
        _height = height;
        _brush = brush;
    }

    public IReadOnlyList<CanvasLayer> Layers => _layers;

    public CanvasLayer? Active { get; private set; }

    public bool Dirty { get; set; }

    public event Action? Changed;

    public CanvasLayer? Find(int id)
    {
        return _layers.Find(l => l.Id == id);
    }

    public CanvasLayer? Add(string name, int? id = null)
    {
        if (_layers.Count >= MaxLayers)
            return null;

        var layerId = id ?? _nextId;
        _nextId = Math.Max(_nextId, layerId + 1);

        var layer = new CanvasLayer(layerId, name, _width, _height, _brush);
        _layers.Add(layer);
        Active = layer;
        Dirty = true;
        Changed?.Invoke();
        return layer;
    }

    public void Remove(CanvasLayer layer)
    {
        if (!_layers.Remove(layer))
            return;

        if (Active == layer)
            Active = null;

        layer.Dispose();
        Dirty = true;
        Changed?.Invoke();
    }

    public void Move(CanvasLayer layer, int delta)
    {
        var index = _layers.IndexOf(layer);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _layers.Count)
            return;

        _layers.RemoveAt(index);
        _layers.Insert(target, layer);
        Dirty = true;
        Changed?.Invoke();
    }

    public void Select(CanvasLayer? layer)
    {
        if (layer != null && !_layers.Contains(layer))
            return;

        Active = layer;
        Changed?.Invoke();
    }

    public void SetVisible(CanvasLayer layer, bool visible)
    {
        layer.Visible = visible;
        Dirty = true;
        Changed?.Invoke();
    }

    public void SetOpacity(CanvasLayer layer, float opacity)
    {
        layer.Opacity = Math.Clamp(opacity, 0f, 1f);
        Dirty = true;
    }

    public Rgba32 Compose(Rgba32 basePixel, int x, int y)
    {
        var result = basePixel;
        foreach (var layer in _layers)
        {
            if (!layer.Visible)
                continue;

            var src = layer.Surface.GetPixelRaw(x, y);
            if (src.A != 0)
                result = PaintSurface.Over(result, src, src.A / 255f * layer.Opacity);
        }

        return result;
    }

    public bool AnyDirty()
    {
        if (Dirty)
            return true;

        foreach (var layer in _layers)
        {
            if (layer.Dirty)
                return true;
        }

        return false;
    }

    public void ClearDirty()
    {
        Dirty = false;
        foreach (var layer in _layers)
        {
            layer.Dirty = false;
        }
    }

    public void Dispose()
    {
        foreach (var layer in _layers)
        {
            layer.Dispose();
        }

        _layers.Clear();
        Active = null;
    }
}
