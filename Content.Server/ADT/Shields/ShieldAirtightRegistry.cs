using System.Collections.Generic;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.ADT.Shields;

/// <summary>
///     Реестр направлений атмосферы, заблокированных сегментами щита. Щит висит в космосе и не
///     может нести AirtightComponent, поэтому направления хранятся здесь, а AtmosphereSystem
///     подмешивает их в данные герметизации тайла. Пуст, пока щит не поднят.
/// </summary>
public static class ShieldAirtightRegistry
{
    private static readonly Dictionary<(EntityUid Grid, Vector2i Tile), Dictionary<AtmosDirection, int>> Blocks = new();

    public static bool HasAny => Blocks.Count > 0;

    public static bool Add(EntityUid grid, Vector2i tile, AtmosDirection dir)
    {
        var before = GetBlockedDirections(grid, tile);

        if (!Blocks.TryGetValue((grid, tile), out var dirs))
        {
            dirs = new Dictionary<AtmosDirection, int>();
            Blocks[(grid, tile)] = dirs;
        }

        dirs[dir] = dirs.GetValueOrDefault(dir) + 1;

        return before != GetBlockedDirections(grid, tile);
    }

    public static bool Remove(EntityUid grid, Vector2i tile, AtmosDirection dir)
    {
        var before = GetBlockedDirections(grid, tile);

        if (Blocks.TryGetValue((grid, tile), out var dirs))
        {
            var count = dirs.GetValueOrDefault(dir);
            if (count <= 1)
            {
                dirs.Remove(dir);
            }
            else
            {
                dirs[dir] = count - 1;
            }

            if (dirs.Count == 0)
                Blocks.Remove((grid, tile));
        }

        return before != GetBlockedDirections(grid, tile);
    }

    public static AtmosDirection GetBlockedDirections(EntityUid grid, Vector2i tile)
    {
        if (!Blocks.TryGetValue((grid, tile), out var dirs))
            return AtmosDirection.Invalid;

        var result = AtmosDirection.Invalid;
        foreach (var (dir, count) in dirs)
        {
            if (count > 0)
                result |= dir;
        }

        return result;
    }

    public static void Clear()
    {
        Blocks.Clear();
    }
}