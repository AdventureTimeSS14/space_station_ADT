using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.UserData;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Cache;

public sealed class CanvasLayerStoreSystem : EntitySystem
{
    private static readonly ResPath Root = ADTUserData.Root / "canvas" / "layers";
    private const string ManifestName = "layers.txt";
    private const int MaxNameLength = 32;

    [Dependency] private readonly IResourceManager _res = default!;

    private readonly List<PendingSave> _saves = new();
    private readonly List<PendingLoad> _loads = new();
    private readonly List<LoadRequest> _deferred = new();

    private sealed record LayerSnapshot(int Id, string Name, bool Visible, float Opacity, Dictionary<int, (Rgba32[] Pixels, Vector2i Size)> Tiles);

    private sealed record LoadRequest(Guid Painting, CanvasLayerStack Stack, int TileCount, Func<int, Vector2i> TileSize);

    private sealed class PendingSave
    {
        public readonly Guid Painting;
        public readonly List<LayerSnapshot> Layers;
        public Task Task = Task.CompletedTask;
        public List<(int Layer, int Index, byte[] Png)>? Result;

        public PendingSave(Guid painting, List<LayerSnapshot> layers)
        {
            Painting = painting;
            Layers = layers;
        }
    }

    private sealed class PendingLoad
    {
        public readonly Guid Painting;
        public readonly CanvasLayerStack Stack;
        public readonly int LayerId;
        public readonly int Index;
        public Task Task = Task.CompletedTask;
        public Rgba32[]? Result;

        public PendingLoad(Guid painting, CanvasLayerStack stack, int layerId, int index)
        {
            Painting = painting;
            Stack = stack;
            LayerId = layerId;
            Index = index;
        }
    }

    public bool IsLoading(Guid painting)
    {
        foreach (var load in _loads)
        {
            if (load.Painting == painting)
                return true;
        }

        foreach (var request in _deferred)
        {
            if (request.Painting == painting)
                return true;
        }

        return false;
    }

    public void Load(Guid painting, CanvasLayerStack stack, int tileCount, Func<int, Vector2i> tileSize)
    {
        var request = new LoadRequest(painting, stack, tileCount, tileSize);
        if (HasPendingSave(painting))
        {
            _deferred.Add(request);
            return;
        }

        StartLoad(request);
    }

    public void Save(Guid painting, CanvasLayerStack stack)
    {
        if (IsLoading(painting))
            return;

        var layers = new List<LayerSnapshot>();
        foreach (var layer in stack.Layers)
        {
            var tiles = new Dictionary<int, (Rgba32[], Vector2i)>();
            foreach (var index in layer.Surface.AllocatedTiles())
            {
                tiles[index] = ((Rgba32[]) layer.Surface.GetTilePixels(index).Clone(), layer.Surface.GetTileSize(index));
            }

            layers.Add(new LayerSnapshot(layer.Id, layer.Name, layer.Visible, layer.Opacity, tiles));
        }

        stack.ClearDirty();

        var save = new PendingSave(painting, layers);
        save.Task = Task.Run(() =>
        {
            save.Result = Encode(layers);
        });
        _saves.Add(save);
    }

    public void Delete(Guid painting)
    {
        _deferred.RemoveAll(r => r.Painting == painting);
        _saves.RemoveAll(s => s.Painting == painting);

        try
        {
            var dir = DirOf(painting);
            if (_res.UserData.Exists(dir))
                _res.UserData.Delete(dir);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to delete canvas layers of {painting}: {e.Message}");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_loads.Count > 0)
            PumpLoads();

        if (_saves.Count > 0)
            PumpSaves();

        if (_deferred.Count > 0)
            PumpDeferred();
    }

    private bool HasPendingSave(Guid painting)
    {
        foreach (var save in _saves)
        {
            if (save.Painting == painting)
                return true;
        }

        return false;
    }

    private void StartLoad(LoadRequest request)
    {
        var dir = DirOf(request.Painting);
        var manifest = dir / ManifestName;
        if (!_res.UserData.Exists(manifest))
            return;

        var stack = request.Stack;
        try
        {
            using (var reader = _res.UserData.OpenText(manifest))
            {
                while (reader.ReadLine() is { } line)
                {
                    var parts = line.Split('\t', 4);
                    if (parts.Length < 4 ||
                        !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ||
                        !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity) ||
                        !float.IsFinite(opacity) ||
                        id <= 0 ||
                        stack.Find(id) != null)
                    {
                        continue;
                    }

                    var name = parts[3].Length > MaxNameLength ? parts[3].Substring(0, MaxNameLength) : parts[3];
                    var layer = stack.Add(name, id);
                    if (layer == null)
                        break;

                    layer.Visible = parts[1] == "1";
                    layer.Opacity = Math.Clamp(opacity, 0f, 1f);
                }
            }

            stack.Select(null);

            foreach (var layer in stack.Layers)
            {
                var layerDir = dir / layer.Id.ToString(CultureInfo.InvariantCulture);
                if (!_res.UserData.IsDir(layerDir))
                    continue;

                foreach (var entry in _res.UserData.DirectoryEntries(layerDir))
                {
                    if (!entry.EndsWith(".png") ||
                        !int.TryParse(entry.Substring(0, entry.Length - 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) ||
                        index < 0 ||
                        index >= request.TileCount)
                    {
                        continue;
                    }

                    var data = ReadAll(layerDir / entry);
                    var size = request.TileSize(index);
                    var load = new PendingLoad(request.Painting, stack, layer.Id, index);
                    load.Task = Task.Run(() =>
                    {
                        load.Result = Decode(data, size);
                    });
                    _loads.Add(load);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load canvas layers of {request.Painting}: {e}");
        }

        stack.ClearDirty();
    }

    private void PumpLoads()
    {
        for (var i = _loads.Count - 1; i >= 0; i--)
        {
            var load = _loads[i];
            if (!load.Task.IsCompleted)
                continue;

            _loads.RemoveAt(i);
            if (load.Result is { } pixels &&
                load.Stack.Find(load.LayerId) is { } layer &&
                !layer.Surface.InStroke)
            {
                layer.Surface.AdoptTile(load.Index, pixels);
            }
        }
    }

    private void PumpSaves()
    {
        while (_saves.Count > 0)
        {
            var save = _saves[0];
            if (!save.Task.IsCompleted)
                return;

            _saves.RemoveAt(0);
            if (save.Result is not { } tiles)
            {
                Log.Error($"Failed to encode canvas layers of {save.Painting}");
                continue;
            }

            Write(save.Painting, save.Layers, tiles);
        }
    }

    private void PumpDeferred()
    {
        for (var i = _deferred.Count - 1; i >= 0; i--)
        {
            var request = _deferred[i];
            if (HasPendingSave(request.Painting))
                continue;

            _deferred.RemoveAt(i);
            StartLoad(request);
        }
    }

    private void Write(Guid painting, List<LayerSnapshot> layers, List<(int Layer, int Index, byte[] Png)> tiles)
    {
        var dir = DirOf(painting);
        var temp = Root / (painting.ToString("N") + ".tmp");

        try
        {
            if (_res.UserData.Exists(temp))
                _res.UserData.Delete(temp);

            if (layers.Count == 0)
            {
                if (_res.UserData.Exists(dir))
                    _res.UserData.Delete(dir);

                return;
            }

            _res.UserData.CreateDir(temp);

            var manifest = new StringBuilder();
            foreach (var layer in layers)
            {
                var name = layer.Name.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
                manifest.Append(layer.Id.ToString(CultureInfo.InvariantCulture));
                manifest.Append('\t');
                manifest.Append(layer.Visible ? "1" : "0");
                manifest.Append('\t');
                manifest.Append(layer.Opacity.ToString("0.###", CultureInfo.InvariantCulture));
                manifest.Append('\t');
                manifest.Append(name);
                manifest.Append('\n');
                _res.UserData.CreateDir(temp / layer.Id.ToString(CultureInfo.InvariantCulture));
            }

            _res.UserData.WriteAllText(temp / ManifestName, manifest.ToString());

            var files = new List<ResPath>(tiles.Count);
            foreach (var (layer, index, png) in tiles)
            {
                var path = new ResPath(layer.ToString(CultureInfo.InvariantCulture)) / (index.ToString(CultureInfo.InvariantCulture) + ".png");
                using var file = _res.UserData.Open(temp / path, FileMode.Create, FileAccess.Write, FileShare.None);
                file.Write(png, 0, png.Length);
                files.Add(path);
            }

            if (_res.UserData.Exists(dir))
                _res.UserData.Delete(dir);

            _res.UserData.CreateDir(dir);
            foreach (var layer in layers)
            {
                _res.UserData.CreateDir(dir / layer.Id.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var path in files)
            {
                _res.UserData.Rename(temp / path, dir / path);
            }

            _res.UserData.Rename(temp / ManifestName, dir / ManifestName);
            _res.UserData.Delete(temp);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save canvas layers of {painting}: {e}");
        }
    }

    private static List<(int Layer, int Index, byte[] Png)> Encode(List<LayerSnapshot> layers)
    {
        var result = new List<(int, int, byte[])>();
        foreach (var layer in layers)
        {
            foreach (var (index, (pixels, size)) in layer.Tiles)
            {
                var empty = true;
                foreach (var pixel in pixels)
                {
                    if (pixel.A != 0)
                    {
                        empty = false;
                        break;
                    }
                }

                if (!empty)
                    result.Add((layer.Id, index, CanvasTileCodec.EncodePng(pixels, size.X, size.Y)));
            }
        }

        return result;
    }

    private static Rgba32[]? Decode(byte[] data, Vector2i size)
    {
        var pixels = new Rgba32[size.X * size.Y];
        return CanvasTileCodec.TryDecodePng(data, size.X, size.Y, pixels) ? pixels : null;
    }

    private byte[] ReadAll(ResPath path)
    {
        using var file = _res.UserData.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var memory = new MemoryStream();
        file.CopyTo(memory);
        return memory.ToArray();
    }

    private static ResPath DirOf(Guid painting)
    {
        return Root / painting.ToString("N");
    }
}
