using Content.Shared.ADT.Legion;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Legion;

public sealed class ADTLegionSpawnerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLegionSpawnerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ADTLegionSpawnerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !ent.Comp.DeleteOnDeath)
            return;

        QueueDel(ent);
    }

    private void OnMapInit(Entity<ADTLegionSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLegionSpawnerComponent>();

        while (query.MoveNext(out var uid, out var spawner))
        {
            if (now < spawner.NextSpawn)
                continue;

            spawner.NextSpawn = now + spawner.Interval;

            if (_mobState.IsDead(uid) || spawner.Prototypes.Count == 0)
                continue;

            spawner.Spawned.RemoveAll(mob => TerminatingOrDeleted(mob) || _mobState.IsDead(mob));

            if (spawner.Spawned.Count >= spawner.MaxMobs)
                continue;

            SpawnMob((uid, spawner));
        }
    }

    private void SpawnMob(Entity<ADTLegionSpawnerComponent> ent)
    {
        var mob = Spawn(_random.Pick(ent.Comp.Prototypes), _transform.GetMoverCoordinates(ent));
        ent.Comp.Spawned.Add(mob);

        if (TryComp<NpcFactionMemberComponent>(ent, out var factions))
        {
            _faction.ClearFactions(mob, false);
            _faction.AddFactions(mob, factions.Factions);
        }

        _popup.PopupEntity(Loc.GetString(ent.Comp.SpawnMessage, ("mob", mob), ("spawner", ent.Owner)), ent, PopupType.Medium);
    }
}
