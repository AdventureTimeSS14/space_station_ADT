using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Generation.Caves;
using Robust.Shared.Map.Components;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    [Dependency] private ADTLavalandCaveSystem _caves = default!;

    protected override bool TryGetCaveEntity(Vector2i indices, Entity<MapGridComponent>? grid, [NotNullWhen(true)] out string? entity)
    {
        entity = null;
        return grid != null && _caves.TryGetWall(grid.Value.Owner, indices, out entity);
    }
}
