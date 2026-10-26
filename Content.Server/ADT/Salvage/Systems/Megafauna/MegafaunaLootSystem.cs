using Content.Server.Storage.EntitySystems;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Mobs;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class MegafaunaLootSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MegafaunaLootComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<MegafaunaLootComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!ent.Comp.DropOnDeath)
            return;

        DropLoot(ent);
    }

    public void DropLoot(Entity<MegafaunaLootComponent> ent)
    {
        if (ent.Comp.LootDropped || TerminatingOrDeleted(ent.Owner))
            return;

        ent.Comp.LootDropped = true;

        var coords = Transform(ent).Coordinates;
        if (!coords.IsValid(EntityManager))
            return;

        EntityUid? crate = null;
        if (ent.Comp.CrateProto is { } crateProto)
            crate = Spawn(crateProto, coords);

        foreach (var entry in ent.Comp.Loot)
        {
            var amount = entry.Min >= entry.Max ? entry.Min : _random.Next(entry.Min, entry.Max + 1);
            SpawnAmount(crate, entry.Proto, amount, coords);
        }

        if (ent.Comp.RandomLoot.Count > 0)
            SpawnInto(crate, _random.Pick(ent.Comp.RandomLoot), coords);

        if (ent.Comp.DropEffect is { } effect)
            Spawn(effect, crate is { } uid ? Transform(uid).Coordinates : coords);

        if (ent.Comp.DropSound is { } sound)
            _audio.PlayPvs(sound, crate ?? ent.Owner);

        if (ent.Comp.DeleteOnDrop)
            QueueDel(ent.Owner);
    }

    private void SpawnAmount(EntityUid? crate, string proto, int amount, EntityCoordinates coords)
    {
        if (amount <= 0)
            return;

        var spawned = SpawnInto(crate, proto, coords);
        if (spawned == null)
            return;

        if (TryComp<StackComponent>(spawned.Value, out var stack))
        {
            _stack.SetCount((spawned.Value, stack), amount);
            return;
        }

        for (var i = 1; i < amount; i++)
        {
            SpawnInto(crate, proto, coords);
        }
    }

    private EntityUid? SpawnInto(EntityUid? crate, string proto, EntityCoordinates coords)
    {
        var item = Spawn(proto, coords);

        if (crate != null && !_entityStorage.Insert(item, crate.Value))
            _transform.SetCoordinates(item, coords);

        return item;
    }
}
