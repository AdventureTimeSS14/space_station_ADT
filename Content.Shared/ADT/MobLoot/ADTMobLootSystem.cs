using Content.Shared.Destructible;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.ADT.MobLoot;

public sealed class ADTMobLootSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMobLootComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ADTMobLootComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<ADTMobLootComponent, ButcherSpawnsModifyEvent>(OnButcherModify);
        SubscribeLocalEvent<ADTMobLootComponent, EntityTerminatingEvent>(OnTerminating);
    }

    private void OnMobStateChanged(Entity<ADTMobLootComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        Roll(ent);
    }

    private void OnDestruction(Entity<ADTMobLootComponent> ent, ref DestructionEventArgs args)
    {
        Roll(ent);
    }

    private void OnButcherModify(Entity<ADTMobLootComponent> ent, ref ButcherSpawnsModifyEvent args)
    {
        if (_net.IsClient)
            return;

        Roll(ent);

        if (ent.Comp.Pending.Count == 0)
            return;

        args.Spawns.AddRange(ent.Comp.Pending);
        ent.Comp.Pending.Clear();
    }

    private void OnTerminating(Entity<ADTMobLootComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient || ent.Comp.Pending.Count == 0)
            return;

        var xform = Transform(ent);

        if (TerminatingOrDeleted(xform.ParentUid))
            return;

        var coordinates = xform.Coordinates;

        foreach (var proto in ent.Comp.Pending)
        {
            Spawn(proto, coordinates);
        }

        ent.Comp.Pending.Clear();
    }

    private void Roll(Entity<ADTMobLootComponent> ent)
    {
        if (_net.IsClient || ent.Comp.Rolled)
            return;

        ent.Comp.Rolled = true;

        foreach (var (proto, chance) in ent.Comp.Loots)
        {
            if (chance <= 0f)
                continue;

            if (chance < 1f && !_random.Prob(chance))
                continue;

            ent.Comp.Pending.Add(proto);
        }
    }
}
