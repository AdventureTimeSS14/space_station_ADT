using System.Linq;
using System.Numerics;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Interaction;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class TendrilSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private const string TargetKey = "Target";
    private const string TargetCoordinatesKey = "TargetCoordinates";
    private const string AggroVisionRadiusKey = "AggroVisionRadius";

    private const int AttackerSearchDepth = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TendrilComponent, TendrilMobDeadEvent>(OnTendrilMobDeath);
        SubscribeLocalEvent<TendrilComponent, DestructionEventArgs>(OnTendrilDestruction);
        SubscribeLocalEvent<TendrilComponent, ComponentStartup>(OnTendrilStartup);
        SubscribeLocalEvent<TendrilComponent, DamageChangedEvent>(OnTendrilDamaged);
        SubscribeLocalEvent<TendrilComponent, AttackedEvent>(OnTendrilAttacked);
        SubscribeLocalEvent<TendrilMobComponent, MobStateChangedEvent>(OnMobState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TendrilComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Aggressor is { } aggressor && (_time.CurTime > comp.AggroEndTime || !IsValidTarget(aggressor)))
                ClearAggro((uid, comp));

            if (comp.Mobs.Count >= comp.MaxSpawns)
                continue;
            if (comp.LastSpawn + TimeSpan.FromSeconds(comp.SpawnDelay) > _time.CurTime)
                continue;

            var xform = Transform(uid);
            var coords = xform.Coordinates;
            var newCoords = coords.Offset(_random.NextVector2(4));
            for (var i = 0; i < 20; i++)
            {
                var randVector = _random.NextVector2(4);
                newCoords = coords.Offset(randVector);
                if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
                {
                    break;
                }
            }
            var mob = Spawn(_random.Pick(comp.Spawns), newCoords);
            var mobComp = EnsureComp<TendrilMobComponent>(mob);
            mobComp.Tendril = uid;
            comp.Mobs.Add(mob);
            comp.LastSpawn = _time.CurTime;

            if (comp.Aggressor is { } current)
                AggroMob((mob, mobComp), current, comp);
        }
    }

    private void OnTendrilStartup(EntityUid uid, TendrilComponent comp, ComponentStartup args)
    {
        comp.LastSpawn = _time.CurTime;
    }

    private void OnTendrilMobDeath(EntityUid uid, TendrilComponent comp, ref TendrilMobDeadEvent args)
    {
        comp.Mobs.Remove(args.Entity);
    }

    private void OnTendrilDamaged(Entity<TendrilComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.Origin is not { } origin)
            return;

        Aggro(ent, origin);
    }

    private void OnTendrilAttacked(Entity<TendrilComponent> ent, ref AttackedEvent args)
    {
        Aggro(ent, args.User);
    }

    private void OnTendrilDestruction(EntityUid uid, TendrilComponent comp, DestructionEventArgs args)
    {
        var coords = Transform(uid).Coordinates;
        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(comp.ChasmDelay), () =>
        {
            SpawnChasm(coords, 2);
        });
    }

    public void Aggro(Entity<TendrilComponent> ent, EntityUid origin)
    {
        if (ResolveAttacker(origin) is not { } attacker)
            return;

        if (!IsValidTarget(attacker))
            return;

        if (ent.Comp.Aggressor is { } previous && previous != attacker)
            ClearAggro(ent);

        ent.Comp.Aggressor = attacker;
        ent.Comp.AggroEndTime = _time.CurTime + ent.Comp.AggroMemory;

        foreach (var mob in ent.Comp.Mobs)
        {
            if (!TryComp<TendrilMobComponent>(mob, out var mobComp))
                continue;

            AggroMob((mob, mobComp), attacker, ent.Comp);
        }
    }

    private void AggroMob(Entity<TendrilMobComponent> mob, EntityUid target, TendrilComponent tendril)
    {
        if (TerminatingOrDeleted(mob) || mob.Owner == target)
            return;

        if (_mobState.IsDead(mob))
            return;

        if (_faction.IsEntityFriendly(mob.Owner, target))
            return;

        if (GetDistance(mob, target) is not { } distance)
            return;

        _faction.AggroEntity(mob.Owner, target);

        if (!TryComp<HTNComponent>(mob, out var htn))
            return;

        _npc.WakeNPC(mob, htn);

        var blackboard = htn.Blackboard;

        mob.Comp.BaseAggroRadius ??= blackboard.GetValueOrDefault<float>(AggroVisionRadiusKey, EntityManager);

        var wanted = MathF.Min(distance + tendril.AggroRadiusPadding, tendril.MaxAggroRadius);
        blackboard.SetValue(AggroVisionRadiusKey, MathF.Max(mob.Comp.BaseAggroRadius.Value, wanted));

        if (HasLiveTarget(blackboard))
            return;

        blackboard.SetValue(TargetKey, target);
        blackboard.SetValue(TargetCoordinatesKey, new EntityCoordinates(target, Vector2.Zero));
    }

    private void ClearAggro(Entity<TendrilComponent> ent)
    {
        if (ent.Comp.Aggressor is { } aggressor)
        {
            foreach (var mob in ent.Comp.Mobs)
            {
                if (TerminatingOrDeleted(mob))
                    continue;

                _faction.DeAggroEntity(mob, aggressor);

                if (!TryComp<TendrilMobComponent>(mob, out var mobComp))
                    continue;

                if (mobComp.BaseAggroRadius is { } baseRadius && TryComp<HTNComponent>(mob, out var htn))
                    htn.Blackboard.SetValue(AggroVisionRadiusKey, baseRadius);

                mobComp.BaseAggroRadius = null;
            }
        }

        ent.Comp.Aggressor = null;
    }

    private EntityUid? ResolveAttacker(EntityUid origin)
    {
        var current = origin;

        for (var depth = 0; depth < AttackerSearchDepth; depth++)
        {
            if (HasComp<MobStateComponent>(current))
                return current;

            var parent = Transform(current).ParentUid;

            if (!parent.IsValid())
                return null;

            current = parent;
        }

        return null;
    }

    private bool IsValidTarget(EntityUid target)
    {
        if (TerminatingOrDeleted(target))
            return false;

        if (!HasComp<MobStateComponent>(target))
            return false;

        return !_mobState.IsDead(target);
    }

    private bool HasLiveTarget(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var current, EntityManager))
            return false;

        if (!current.IsValid() || TerminatingOrDeleted(current))
            return false;

        return !_mobState.IsDead(current);
    }

    private float? GetDistance(EntityUid uid, EntityUid target)
    {
        var ourCoords = _transform.GetMapCoordinates(uid);
        var targetCoords = _transform.GetMapCoordinates(target);

        if (ourCoords.MapId == MapId.Nullspace || ourCoords.MapId != targetCoords.MapId)
            return null;

        return (ourCoords.Position - targetCoords.Position).Length();
    }

    private void SpawnChasm(EntityCoordinates coords, int radius)
    {
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                Spawn("FloorChasmEntity", new EntityCoordinates(coords.EntityId, coords.X + dx, coords.Y + dy));
            }
        }
    }

    private void OnMobState(EntityUid uid, TendrilMobComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        if (!comp.Tendril.HasValue)
            return;
        var ev = new TendrilMobDeadEvent(uid);
        RaiseLocalEvent(comp.Tendril.Value, ref ev);
    }
}
