using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public static readonly ProtoId<SalvageLootPrototype> ExpeditionsLootProto = "SalvageLoot";

    public string GetFTLName(LocalizedDatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{Loc.GetString(dataset.Values[random.Next(dataset.Values.Count)])}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(SalvageDifficultyPrototype difficulty, int seed)
    {
        var modifierBudget = difficulty.ModifierBudget;
        var rand = new System.Random(seed);

        var biome = GetMod<SalvageBiomeModPrototype>(rand, ref modifierBudget);
        var light = GetBiomeMod<SalvageLightMod>(biome.ID, rand, ref modifierBudget);
        var temp = GetBiomeMod<SalvageTemperatureMod>(biome.ID, rand, ref modifierBudget);
        var air = GetBiomeMod<SalvageAirMod>(biome.ID, rand, ref modifierBudget);
        var dungeon = GetBiomeMod<SalvageDungeonModPrototype>(biome.ID, rand, ref modifierBudget);

        var factionProtos = _proto.EnumeratePrototypes<SalvageFactionPrototype>().ToList();
        factionProtos.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        var faction = factionProtos.Count > 0 ? factionProtos[seed % factionProtos.Count] : _proto.EnumeratePrototypes<SalvageFactionPrototype>().First();

        var mods = new List<string>();

        if (air != null && air.Description != string.Empty)
        {
            mods.Add(Loc.GetString(air.Description));
        }

        if (temp != null && air != null && temp.Description != string.Empty && !air.Space)
        {
            mods.Add(Loc.GetString(temp.Description));
        }

        if (light != null && light.Description != string.Empty)
        {
            mods.Add(Loc.GetString(light.Description));
        }

        var duration = TimeSpan.FromSeconds(CfgManager.GetCVar(CCVars.SalvageExpeditionDuration));

        return new SalvageMission(
            seed,
            dungeon?.ID ?? "SalvageAsteroidDungeon",
            faction.ID,
            biome.ID,
            air?.ID ?? "SpaceAir",
            temp?.Temperature ?? 293.15f,
            light?.Color ?? Color.White,
            duration,
            mods
        );
    }

    private T? GetBiomeMod<T>(Dictionary<string, T> mods, System.Random rand, ref float rating) where T : ISalvageMod
    {
        if (mods.Count == 0)
            return default;

        // Копируем ref-переменную в локальную, чтобы использовать в лямбде
        var currentRating = rating;
        var options = mods.Values.Where(x => x.Cost <= currentRating).ToList();

        if (options.Count == 0)
            options = mods.Values.ToList();

        if (options.Count == 0)
            return default;

        var mod = options[rand.Next(options.Count)];
        rating -= mod.Cost;
        return mod;
    }

    private T GetBiomeMod<T>(string biomeId, System.Random rand, ref float rating) where T : class, IPrototype
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        if (mods.Count == 0)
            throw new InvalidOperationException();

        return mods[rand.Next(mods.Count)];
    }

    public T GetMod<T>(System.Random rand, ref float rating) where T : class, IPrototype, ISalvageMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating)
                continue;

            rating -= mod.Cost;
            return mod;
        }

        if (mods.Count > 0)
        {
            var fallbackMod = mods[rand.Next(mods.Count)];
            rating -= fallbackMod.Cost;
            return fallbackMod;
        }

        return _proto.EnumeratePrototypes<T>().First();
    }
}
