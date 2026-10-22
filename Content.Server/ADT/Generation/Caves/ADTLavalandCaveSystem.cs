using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation.Caves;

public sealed class ADTLavalandCaveSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandCavesComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTLavalandCavesComponent> ent, ref MapInitEvent args)
    {
        var seed = TryComp<BiomeComponent>(ent, out var biome) ? biome.Seed : _random.Next();

        var center = Vector2i.Zero;
        var range = 350;
        if (TryComp<RestrictedRangeComponent>(ent, out var restricted))
        {
            center = new Vector2i((int) MathF.Floor(restricted.Origin.X), (int) MathF.Floor(restricted.Origin.Y));
            range = (int) MathF.Ceiling(restricted.Range);
        }

        var size = range * 2 + 1;
        var origin = center - new Vector2i(range, range);
        var generator = new ADTCaveGenerator(ent.Comp, seed, origin, size);

        ent.Comp.Prototypes = BuildPrototypes(ent.Comp, generator.OreNames);
        ent.Comp.Data = Task.Run(generator.Generate);
    }

    private string[][] BuildPrototypes(ADTLavalandCavesComponent comp, List<string> oreNames)
    {
        var result = new string[comp.Walls.Count][];
        for (var i = 0; i < comp.Walls.Count; i++)
        {
            var wall = comp.Walls[i].Prototype.Id;
            var row = new string[oreNames.Count + 1];
            row[0] = wall;

            for (var ore = 0; ore < oreNames.Count; ore++)
            {
                var id = wall + oreNames[ore];
                if (!_prototype.HasIndex<EntityPrototype>(id))
                {
                    Log.Error($"Lavaland caves: no prototype {id}, falling back to {wall}.");
                    id = wall;
                }

                row[ore + 1] = id;
            }

            result[i] = row;
        }

        return result;
    }

    private static bool TryGetData(ADTLavalandCavesComponent comp, [NotNullWhen(true)] out ADTCaveData? data)
    {
        if (comp.Data == null)
        {
            data = null;
            return false;
        }

        // Генератор крутится в Task.Run и не возвращается в контекст главного потока, взаимоблокировки тут не будет.
#pragma warning disable RA0004
        data = comp.Data.Result;
#pragma warning restore RA0004
        return true;
    }

    public bool TryGetWall(EntityUid map, Vector2i tile, [NotNullWhen(true)] out string? prototype)
    {
        prototype = null;
        if (!TryComp<ADTLavalandCavesComponent>(map, out var comp) || !TryGetData(comp, out var data))
            return false;

        if (!data.TryGetIndex(tile, out var index))
            return false;

        var type = data.Types[index];
        if (type == 0 || type > comp.Prototypes.Length)
            return false;

        prototype = comp.Prototypes[type - 1][data.Ores[index]];
        return true;
    }

    public bool TryGetOreAmount(EntityUid map, Vector2i tile, out int amount)
    {
        amount = 0;
        if (!TryComp<ADTLavalandCavesComponent>(map, out var comp) || !TryGetData(comp, out var data))
            return false;

        if (!data.TryGetIndex(tile, out var index) || data.Ores[index] == 0)
            return false;

        amount = data.Amounts[index];
        return amount > 0;
    }
}
