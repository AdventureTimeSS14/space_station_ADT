using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Robust.Shared.Configuration;

namespace Content.Server.ADT.Drawing;

public sealed class PaintingBlobStoreSystem : EntitySystem
{
    private const int MigrateBatch = 64;

    private static readonly Regex LocationPattern = new("^[0-9a-f]{2}/[0-9a-f]{2}/[0-9a-f]{32}\\.png$", RegexOptions.Compiled);

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private string _mode = "db";
    private string _dir = string.Empty;

    public bool External => _mode == "file" && _dir.Length > 0;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ADTCCVars.CanvasBlobStore, v => _mode = v.Trim().ToLowerInvariant(), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasBlobDir, v => _dir = v.Trim(), true);
    }

    public async Task<Dictionary<CanvasHash, string>?> StoreAsync(Dictionary<CanvasHash, byte[]> blobs)
    {
        if (!External || blobs.Count == 0)
            return null;

        var dir = _dir;
        return await Task.Run(() =>
        {
            var locations = new Dictionary<CanvasHash, string>();
            foreach (var (hash, data) in blobs)
            {
                var location = LocationOf(hash);
                var path = Path.Combine(dir, location);
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    var temp = path + ".tmp";
                    File.WriteAllBytes(temp, data);
                    File.Move(temp, path, true);
                }

                locations[hash] = location;
            }

            return locations;
        });
    }

    public async Task ResolveAsync(Dictionary<CanvasHash, string> external, Dictionary<CanvasHash, byte[]> into)
    {
        if (external.Count == 0)
            return;

        var dir = _dir;
        if (dir.Length == 0)
        {
            Log.Error($"{external.Count} painting blobs are stored in files, but adt.canvas.blob_dir is empty");
            return;
        }

        var loaded = await Task.Run(() =>
        {
            var result = new Dictionary<CanvasHash, byte[]>();
            foreach (var (hash, location) in external)
            {
                if (!LocationPattern.IsMatch(location))
                {
                    Log.Error($"Painting blob {hash} has invalid location {location}");
                    continue;
                }

                var path = Path.Combine(dir, location);
                if (!File.Exists(path))
                {
                    Log.Error($"Painting blob {hash} is missing at {path}");
                    continue;
                }

                var data = File.ReadAllBytes(path);
                if (CanvasHash.Compute(data) != hash)
                {
                    Log.Error($"Painting blob {hash} at {path} is corrupted");
                    continue;
                }

                result[hash] = data;
            }

            return result;
        });

        foreach (var (hash, data) in loaded)
        {
            into[hash] = data;
        }
    }

    public async Task DeleteAsync(List<string> locations)
    {
        if (locations.Count == 0 || _dir.Length == 0)
            return;

        var used = await _db.GetUsedPaintingBlobLocationsAsync(locations);
        var dir = _dir;
        await Task.Run(() =>
        {
            foreach (var location in locations)
            {
                if (used.Contains(location) || !LocationPattern.IsMatch(location))
                    continue;

                try
                {
                    File.Delete(Path.Combine(dir, location));
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to delete painting blob file {location}: {e.Message}");
                }
            }
        });
    }

    public async Task<int> MigrateToFilesAsync()
    {
        if (!External)
            return -1;

        var moved = 0;
        var skipped = 0;
        var seen = new HashSet<CanvasHash>();
        while (true)
        {
            var batch = await _db.GetInlinePaintingBlobsAsync(MigrateBatch, skipped);
            if (batch.Count == 0)
                return moved;

            var blobs = new Dictionary<CanvasHash, byte[]>();
            foreach (var (hash, data) in batch)
            {
                if (!seen.Add(hash))
                    return moved;

                if (CanvasHash.Compute(data) == hash)
                {
                    blobs[hash] = data;
                    continue;
                }

                skipped++;
                Log.Error($"Painting blob {hash} in the database is corrupted, left in place");
            }

            if (blobs.Count == 0)
                continue;

            var locations = await StoreAsync(blobs);
            if (locations == null)
                return moved;

            await _db.SetPaintingBlobLocationsAsync(locations);
            moved += locations.Count;
        }
    }

    public async Task<int> ImportFromFilesAsync()
    {
        if (_dir.Length == 0)
            return -1;

        var moved = 0;
        var skipped = 0;
        var seen = new HashSet<CanvasHash>();
        while (true)
        {
            var batch = await _db.GetExternalPaintingBlobsAsync(MigrateBatch, skipped);
            if (batch.Count == 0)
                return moved;

            foreach (var hash in batch.Keys)
            {
                if (!seen.Add(hash))
                    return moved;
            }

            var loaded = new Dictionary<CanvasHash, byte[]>();
            await ResolveAsync(batch, loaded);
            skipped += batch.Count - loaded.Count;

            if (loaded.Count == 0)
                continue;

            await _db.SetPaintingBlobDataAsync(loaded);
            moved += loaded.Count;

            var locations = new List<string>();
            foreach (var hash in loaded.Keys)
            {
                locations.Add(batch[hash]);
            }

            await DeleteAsync(locations);
        }
    }

    private static string LocationOf(CanvasHash hash)
    {
        var bytes = new byte[CanvasConstants.HashLength];
        hash.WriteTo(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"{hex[..2]}/{hex.Substring(2, 2)}/{hex}.png";
    }
}
