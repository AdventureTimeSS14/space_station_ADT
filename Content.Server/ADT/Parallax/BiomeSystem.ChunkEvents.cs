using Content.Server.ADT.Parallax;
using Content.Shared.Parallax.Biomes;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    public bool IsTileChunkLoaded(BiomeComponent biome, Vector2i tile)
    {
        var chunk = SharedMapSystem.GetChunkIndices(tile, ChunkSize) * ChunkSize;
        return biome.LoadedChunks.Contains(chunk);
    }

    private void RaiseChunkLoaded(EntityUid gridUid, Vector2i chunk)
    {
        var ev = new ADTBiomeChunkLoadedEvent(GetChunkBounds(chunk));
        RaiseLocalEvent(gridUid, ref ev);
    }

    private void RaiseChunkUnloaded(EntityUid gridUid, Vector2i chunk)
    {
        var ev = new ADTBiomeChunkUnloadedEvent(GetChunkBounds(chunk));
        RaiseLocalEvent(gridUid, ref ev);
    }

    private static Box2 GetChunkBounds(Vector2i chunk)
    {
        return new Box2(chunk.X, chunk.Y, chunk.X + ChunkSize, chunk.Y + ChunkSize);
    }
}
