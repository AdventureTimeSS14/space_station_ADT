using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map.Components;

namespace Content.Shared.Parallax.Biomes;

public abstract partial class SharedBiomeSystem
{
    protected virtual bool TryGetCaveEntity(Vector2i indices, Entity<MapGridComponent>? grid, [NotNullWhen(true)] out string? entity)
    {
        entity = null;
        return false;
    }
}
