using System.IO;
using System.Linq;
using System.Text;
using Content.Client.ADT.UserData;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Drawing.Cache;

public sealed class CanvasTileCacheSystem : EntitySystem
{
    private const int FlushEvery = 32;

    private static readonly ResPath Root = ADTUserData.Root / "canvas" / "cache";
    private static readonly ResPath IndexPath = Root / "index.txt";

    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Dictionary<CanvasHash, Entry> _entries = new();
    private ISawmill _sawmill = default!;
    private bool _loaded;
    private long _totalBytes;
    private long _limitBytes;
    private int _unsaved;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("adt.canvas.cache");
        Subs.CVar(_cfg, ADTCCVars.CanvasClientCacheMb, OnLimitChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_unsaved > 0)
            SaveIndex();
    }

    public bool Contains(CanvasHash hash)
    {
        EnsureLoaded();
        return _entries.ContainsKey(hash);
    }

    public bool TryGet(CanvasHash hash, out byte[] data)
    {
        data = Array.Empty<byte>();
        EnsureLoaded();

        if (!_entries.TryGetValue(hash, out var entry))
            return false;

        var path = PathOf(hash);
        try
        {
            using var file = _res.UserData.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Length > CanvasConstants.MaxTileBytes)
                throw new InvalidDataException("Cached tile is too large");

            using var buffer = new MemoryStream();
            file.CopyTo(buffer);
            data = buffer.ToArray();
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Failed to read cached tile {hash}: {e.Message}");
            Remove(hash);
            data = Array.Empty<byte>();
            return false;
        }

        if (CanvasHash.Compute(data) != hash)
        {
            _sawmill.Warning($"Cached tile {hash} has wrong hash, dropping");
            Remove(hash);
            data = Array.Empty<byte>();
            return false;
        }

        entry.LastUse = DateTime.UtcNow.Ticks;
        MarkUnsaved();
        return true;
    }

    public bool Put(CanvasHash hash, byte[] data)
    {
        EnsureLoaded();

        if (data.Length == 0 || data.Length > CanvasConstants.MaxTileBytes || CanvasHash.Compute(data) != hash)
            return false;

        if (_entries.TryGetValue(hash, out var existing))
        {
            existing.LastUse = DateTime.UtcNow.Ticks;
            MarkUnsaved();
            return true;
        }

        var path = PathOf(hash);
        try
        {
            _res.UserData.CreateDir(path.Directory);
            using var file = _res.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            file.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Failed to write cached tile {hash}: {e.Message}");
            return false;
        }

        _entries[hash] = new Entry(data.Length, DateTime.UtcNow.Ticks);
        _totalBytes += data.Length;
        Evict();
        MarkUnsaved();
        return true;
    }

    public void Invalidate(IEnumerable<CanvasHash> hashes)
    {
        EnsureLoaded();

        var removed = false;
        foreach (var hash in hashes)
        {
            removed |= Remove(hash);
        }

        if (removed)
            SaveIndex();
    }

    private void OnLimitChanged(int megabytes)
    {
        _limitBytes = Math.Max(0, megabytes) * 1024L * 1024L;
        if (!_loaded)
            return;

        if (Evict())
            SaveIndex();
    }

    private bool Evict()
    {
        if (_totalBytes <= _limitBytes)
            return false;

        var evicted = false;
        foreach (var (hash, _) in _entries.OrderBy(e => e.Value.LastUse).ToList())
        {
            if (_totalBytes <= _limitBytes)
                break;

            evicted |= Remove(hash);
        }

        return evicted;
    }

    private bool Remove(CanvasHash hash)
    {
        var path = PathOf(hash);
        try
        {
            if (_res.UserData.Exists(path))
                _res.UserData.Delete(path);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Failed to delete cached tile {hash}: {e.Message}");
        }

        if (!_entries.Remove(hash, out var entry))
            return false;

        _totalBytes -= entry.Size;
        MarkUnsaved();
        return true;
    }

    private void MarkUnsaved()
    {
        _unsaved++;
        if (_unsaved >= FlushEvery)
            SaveIndex();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        _entries.Clear();
        _totalBytes = 0;

        try
        {
            if (!_res.UserData.Exists(IndexPath))
                return;

            using var reader = _res.UserData.OpenText(IndexPath);
            while (reader.ReadLine() is { } line)
            {
                var parts = line.Split(' ');
                if (parts.Length != 3 ||
                    !CanvasHash.TryParseHex(parts[0], out var hash) ||
                    !int.TryParse(parts[1], out var size) ||
                    !long.TryParse(parts[2], out var lastUse) ||
                    size <= 0 ||
                    size > CanvasConstants.MaxTileBytes ||
                    _entries.ContainsKey(hash))
                {
                    continue;
                }

                _entries[hash] = new Entry(size, lastUse);
                _totalBytes += size;
            }
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Failed to read cache index, starting empty: {e.Message}");
            _entries.Clear();
            _totalBytes = 0;
        }

        if (Evict())
            SaveIndex();
    }

    private void SaveIndex()
    {
        _unsaved = 0;

        var builder = new StringBuilder();
        foreach (var (hash, entry) in _entries)
        {
            builder.Append(hash.ToHex());
            builder.Append(' ');
            builder.Append(entry.Size);
            builder.Append(' ');
            builder.Append(entry.LastUse);
            builder.Append('\n');
        }

        try
        {
            _res.UserData.CreateDir(Root);
            _res.UserData.WriteAllText(IndexPath, builder.ToString());
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Failed to save cache index: {e.Message}");
        }
    }

    private static ResPath PathOf(CanvasHash hash)
    {
        var hex = hash.ToHex();
        return Root / hex.Substring(0, 2) / (hex + ".png");
    }

    private sealed class Entry
    {
        public readonly int Size;
        public long LastUse;

        public Entry(int size, long lastUse)
        {
            Size = size;
            LastUse = lastUse;
        }
    }
}
