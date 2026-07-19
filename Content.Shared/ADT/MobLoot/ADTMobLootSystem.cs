using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Shared.ADT.MobLoot;

public sealed class ADTMobLootSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMobLootComponent, ButcherSpawnsModifyEvent>(OnButcherModify);
    }

    private void OnButcherModify(Entity<ADTMobLootComponent> ent, ref ButcherSpawnsModifyEvent args)
    {
        if (ent.Comp.Loots.Count == 0)
            return;

        foreach (var (proto, chance) in ent.Comp.Loots)
        {
            if (chance <= 0f)
                continue;

            if (chance < 1f && !_random.Prob(chance))
                continue;

            args.Spawns.Add(proto);
        }
    }
}
