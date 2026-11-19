using Content.Shared.ADT.Drawing;
using Robust.Shared.Network;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class CanvasLayer : IDisposable
{
    public readonly int Id;
    public readonly bool Own;
    public readonly PaintSurface Surface;
    public readonly BrushStroker Stroker;

    public readonly uint[] Versions;
    public readonly uint[] Synced;
    public readonly bool[] InFlight;
    public readonly uint[] InFlightRevision;
    public readonly Dictionary<int, List<uint>> PendingStrokes = new();

    public string Name;
    public bool Visible = true;
    public bool LocalHidden;
    public float Opacity = 1f;
    public bool Dirty;
    public byte ServerId;
    public NetUserId? Owner;
    public int OwnerLocalId;
    public string OwnerName = string.Empty;
    public bool CreateSent;
    public sbyte LastMove;
    public bool OpacityPending;
    public bool SyncPending;
    public bool UseServerCopy;

    public CanvasLayer(int id, bool own, string name, int width, int height, BrushSettings brush)
    {
        Id = id;
        Own = own;
        Name = name;
        Surface = new PaintSurface(width, height, Color.Transparent, sparse: true)
        {
            EraseToPaper = false,
            UndoBudget = 64L * 1024 * 1024,
            History = own,
        };
        Stroker = new BrushStroker(Surface, brush);

        Versions = new uint[Surface.TileCount];
        Synced = new uint[Surface.TileCount];
        InFlight = new bool[Surface.TileCount];
        InFlightRevision = new uint[Surface.TileCount];

        if (!own)
            return;

        Stroker.StrokeFinished += OnStrokeFinished;
        Surface.HistoryChanged += MarkEdited;
    }

    public bool Shown => Visible && !LocalHidden;

    public bool Registered => ServerId != CanvasConstants.BaseLayer;

    public event Action? Edited;

    private void OnStrokeFinished(StrokeRecord stroke, int tag, int[] tiles)
    {
        MarkEdited();
        if (tag == 0)
            return;

        foreach (var index in tiles)
        {
            if (!PendingStrokes.TryGetValue(index, out var ids))
            {
                ids = new List<uint>();
                PendingStrokes[index] = ids;
            }

            if (ids.Count < CanvasConstants.MaxStrokeIds)
                ids.Add((uint) tag);
        }
    }

    public void MarkEdited()
    {
        Dirty = true;
        SyncPending = true;
        Edited?.Invoke();
    }

    public bool TileModified(int index)
    {
        return !InFlight[index] && Surface.TileRevision(index) != Synced[index];
    }

    public bool HasPendingWork()
    {
        if (!Own || !Registered)
            return false;

        for (var i = 0; i < InFlight.Length; i++)
        {
            if (InFlight[i] || Surface.TileRevision(i) != Synced[i])
                return true;
        }

        return false;
    }

    public void MarkSynced(int index)
    {
        Synced[index] = Surface.TileRevision(index);
    }

    public void MarkAllSynced()
    {
        for (var i = 0; i < Synced.Length; i++)
        {
            MarkSynced(i);
        }
    }

    public void Dispose()
    {
        Surface.Dispose();
    }
}

public sealed class CanvasLayerStack : IDisposable
{
    public const int MaxLayers = CanvasConstants.MaxLayersPerMember;

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

    public bool IsHost { get; set; }

    public HashSet<(Guid Owner, int LocalId)> HiddenRemote { get; } = new();

    public event Action? Changed;

    public event Action<CanvasLayer>? LayerAdded;

    public event Action<CanvasLayerActionKind, CanvasLayer>? ActionRequested;

    public int OwnCount()
    {
        var count = 0;
        foreach (var layer in _layers)
        {
            if (layer.Own)
                count++;
        }

        return count;
    }

    public CanvasLayer? Find(int id)
    {
        return _layers.Find(l => l.Id == id);
    }

    public CanvasLayer? FindServer(byte serverId)
    {
        if (serverId == CanvasConstants.BaseLayer)
            return null;

        return _layers.Find(l => l.ServerId == serverId);
    }

    public CanvasLayer? Add(string name, int? id = null)
    {
        if (OwnCount() >= MaxLayers)
            return null;

        var layerId = id ?? _nextId;
        _nextId = Math.Max(_nextId, layerId + 1);

        var layer = new CanvasLayer(layerId, true, name, _width, _height, _brush);
        _layers.Add(layer);
        Active = layer;
        Dirty = true;
        LayerAdded?.Invoke(layer);
        ActionRequested?.Invoke(CanvasLayerActionKind.Create, layer);
        Changed?.Invoke();
        return layer;
    }

    public bool CanManage(CanvasLayer layer)
    {
        return layer.Own || IsHost && layer.Registered;
    }

    public void Remove(CanvasLayer layer)
    {
        if (!_layers.Contains(layer) || !CanManage(layer))
            return;

        ActionRequested?.Invoke(CanvasLayerActionKind.Delete, layer);
        Detach(layer);
    }

    private void Detach(CanvasLayer layer)
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
        if (!layer.Own)
            return;

        var index = _layers.IndexOf(layer);
        var target = index + Math.Sign(delta);
        if (index < 0 || delta == 0 || target < 0 || target >= _layers.Count)
            return;

        _layers.RemoveAt(index);
        _layers.Insert(target, layer);
        layer.LastMove = (sbyte) Math.Sign(delta);
        Dirty = true;
        ActionRequested?.Invoke(CanvasLayerActionKind.Move, layer);
        Changed?.Invoke();
    }

    public void Select(CanvasLayer? layer)
    {
        if (layer != null && (!_layers.Contains(layer) || !layer.Own))
            return;

        Active = layer;
        Changed?.Invoke();
    }

    public void SetVisible(CanvasLayer layer, bool visible)
    {
        if (!CanManage(layer) || layer.Visible == visible)
            return;

        layer.Visible = visible;
        Dirty = true;
        ActionRequested?.Invoke(CanvasLayerActionKind.Visible, layer);
        Changed?.Invoke();
    }

    public void SetLocalHidden(CanvasLayer layer, bool hidden)
    {
        if (layer.LocalHidden == hidden)
            return;

        layer.LocalHidden = hidden;
        if (!layer.Own && layer.Owner is { } owner)
        {
            if (hidden)
                HiddenRemote.Add((owner.UserId, layer.OwnerLocalId));
            else
                HiddenRemote.Remove((owner.UserId, layer.OwnerLocalId));
        }

        Dirty = true;
        Changed?.Invoke();
    }

    public void SetOpacity(CanvasLayer layer, float opacity)
    {
        if (!layer.Own)
            return;

        layer.Opacity = Math.Clamp(opacity, 0f, 1f);
        layer.OpacityPending = true;
        Dirty = true;
    }

    public void ApplyServer(List<CanvasLayerInfo> infos, NetUserId? local, bool manifest, Func<NetUserId, string> ownerName)
    {
        var ordered = new List<CanvasLayer>(infos.Count + _layers.Count);
        var seen = new HashSet<CanvasLayer>();

        foreach (var info in infos)
        {
            CanvasLayer? layer;
            if (info.Owner == local)
            {
                layer = _layers.Find(l => l.Own && l.Id == info.LocalId);
                if (layer == null)
                {
                    layer = new CanvasLayer(info.LocalId, true, info.Name, _width, _height, _brush)
                    {
                        UseServerCopy = true,
                    };
                    _nextId = Math.Max(_nextId, info.LocalId + 1);
                    Dirty = true;
                    LayerAdded?.Invoke(layer);
                }
                else if (manifest && !layer.Registered)
                {
                    layer.UseServerCopy = true;
                    layer.Surface.ReleaseAll();
                    layer.PendingStrokes.Clear();
                    layer.MarkAllSynced();
                }
            }
            else
            {
                layer = _layers.Find(l => !l.Own && l.ServerId == info.Id);
                if (layer == null)
                {
                    layer = new CanvasLayer(-info.Id, false, info.Name, _width, _height, _brush)
                    {
                        LocalHidden = HiddenRemote.Contains((info.Owner.UserId, info.LocalId)),
                    };
                    LayerAdded?.Invoke(layer);
                }
            }

            if (!seen.Add(layer))
                continue;

            layer.ServerId = info.Id;
            layer.Owner = info.Owner;
            layer.OwnerLocalId = info.LocalId;
            layer.OwnerName = ownerName(info.Owner);
            layer.Name = info.Name;
            layer.Visible = info.Visible;
            if (!layer.OpacityPending)
                layer.Opacity = info.Opacity;

            ordered.Add(layer);
        }

        foreach (var layer in _layers)
        {
            if (seen.Contains(layer))
                continue;

            if (layer.Own && !layer.Registered)
            {
                ordered.Add(layer);
                continue;
            }

            if (Active == layer)
                Active = null;

            layer.Dispose();
            Dirty = true;
        }

        _layers.Clear();
        _layers.AddRange(ordered);
        Changed?.Invoke();
    }

    public void RefreshOwnerNames(Func<NetUserId, string> ownerName)
    {
        foreach (var layer in _layers)
        {
            if (layer.Owner is { } owner)
                layer.OwnerName = ownerName(owner);
        }

        Changed?.Invoke();
    }

    public Rgba32 Compose(Rgba32 basePixel, int x, int y)
    {
        var result = basePixel;
        foreach (var layer in _layers)
        {
            if (!layer.Shown)
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
            if (layer.Own && layer.Dirty)
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
