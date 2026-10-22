using System.Numerics;
using Content.Server.NPC.HTN;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed class ADTNpcCombatMemorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;

    private readonly HashSet<Entity<NpcFactionMemberComponent>> _factionMembers = new();
    private readonly HashSet<Entity<EntityStorageComponent>> _storages = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(EntityUid uid, ADTNpcCombatMemoryComponent component, EntitySpokeEvent args)
    {
        component.Spoke = true;
        component.LastSpoke = _timing.CurTime;
    }

    private void OnShotAttempted(EntityUid uid, ADTNpcCombatMemoryComponent component, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !HasComp<ActiveNPCComponent>(uid))
            return;

        if (args.Used.Comp.ShootCoordinates is not { } coordinates)
            return;

        var from = _transform.GetMapCoordinates(uid);
        var to = _transform.ToMapCoordinates(coordinates);

        if (from.MapId != to.MapId)
            return;

        var segment = to.Position - from.Position;
        var lengthSq = segment.LengthSquared();

        if (lengthSq < 0.01f)
            return;

        _factionMembers.Clear();
        _lookup.GetEntitiesInRange(from, MathF.Sqrt(lengthSq) + 0.5f, _factionMembers);

        foreach (var found in _factionMembers)
        {
            var ally = found.Owner;

            if (ally == uid || _mobState.IsIncapacitated(ally) || !_faction.IsEntityFriendly(uid, ally))
                continue;

            var pos = _transform.GetMapCoordinates(ally).Position;
            var along = Vector2.Dot(pos - from.Position, segment) / lengthSq;

            if (along <= 0f || along >= 1f)
                continue;

            if (Vector2.Distance(from.Position + segment * along, pos) > component.AllyClearance)
                continue;

            args.Cancel();
            component.Blocked = true;
            component.LastBlocked = _timing.CurTime;
            return;
        }
    }

    private void OnDamageDealt(EntityUid uid, ADTNpcCombatMemoryComponent component, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive() || args.Origin == null || args.Origin == uid)
            return;

        component.Hurt = true;
        component.LastHurt = _timing.CurTime;
    }

    public bool HasHostileNear(EntityUid owner, float range)
    {
        if (!TryComp<ADTNpcCombatMemoryComponent>(owner, out var memory))
            return NearestHostile(owner, range) <= range;

        var tick = _timing.CurTick;

        if (memory.HostileTick != tick || memory.HostileScannedRange < range)
        {
            var scan = MathF.Max(range, memory.HostileScanRange);
            memory.NearestHostile = NearestHostile(owner, scan);
            memory.HostileScannedRange = scan;
            memory.HostileTick = tick;
        }

        return memory.NearestHostile <= range;
    }

    private float NearestHostile(EntityUid owner, float range)
    {
        var ownerPos = _transform.GetMapCoordinates(owner);
        var nearest = float.MaxValue;

        foreach (var hostile in _faction.GetNearbyHostiles(owner, range))
        {
            if (_mobState.IsIncapacitated(hostile))
                continue;

            var pos = _transform.GetMapCoordinates(hostile);

            if (pos.MapId != ownerPos.MapId)
                continue;

            var dist = Vector2.Distance(ownerPos.Position, pos.Position);

            if (dist < nearest)
                nearest = dist;
        }

        return nearest;
    }

    public EntityUid FindLeader(EntityUid owner, float range)
    {
        if (!TryComp<ADTNpcCombatMemoryComponent>(owner, out var memory))
            return ScanLeader(owner, range);

        var now = _timing.CurTime;

        if (now < memory.NextLeaderCheck && memory.Leader is { } cached &&
            (cached == owner || !TerminatingOrDeleted(cached) && _mobState.IsAlive(cached)))
        {
            return cached;
        }

        var leader = ScanLeader(owner, range);
        memory.Leader = leader;
        memory.NextLeaderCheck = now + memory.LeaderCheckInterval;
        return leader;
    }

    private EntityUid ScanLeader(EntityUid owner, float range)
    {
        var leader = owner;

        _factionMembers.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(owner), range, _factionMembers);

        foreach (var found in _factionMembers)
        {
            var other = found.Owner;

            if (other == owner || !HasComp<HTNComponent>(other))
                continue;

            if (!_mobState.IsAlive(other) || !_faction.IsEntityFriendly(owner, other))
                continue;

            if (other.CompareTo(leader) < 0)
                leader = other;
        }

        return leader;
    }

    public bool TryBeginCoverSearch(EntityUid owner, TimeSpan cooldown)
    {
        if (!TryComp<ADTNpcCombatMemoryComponent>(owner, out var memory))
            return true;

        var now = _timing.CurTime;

        if (now < memory.NextCoverSearch)
            return false;

        memory.NextCoverSearch = now + cooldown;
        return true;
    }

    public EntityUid? FindObstacle(EntityUid owner, float range, float lineWidth, bool openable)
    {
        if (openable || !TryComp<ADTNpcCombatMemoryComponent>(owner, out var memory))
            return ScanObstacle(owner, range, lineWidth, openable);

        var tick = _timing.CurTick;

        if (memory.ObstacleTick != tick)
        {
            memory.Obstacle = ScanObstacle(owner, range, lineWidth, false);
            memory.ObstacleTick = tick;
        }

        return memory.Obstacle;
    }

    private EntityUid? ScanObstacle(EntityUid owner, float range, float lineWidth, bool openable)
    {
        var ownerMap = _transform.GetMapCoordinates(owner);
        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var hostile in _faction.GetNearbyHostiles(owner, range))
        {
            if (_mobState.IsIncapacitated(hostile))
                continue;

            if (_containers.TryGetContainingContainer(hostile, out var container))
            {
                if (!HasComp<EntityStorageComponent>(container.Owner))
                    continue;

                if (_entityStorage.CanOpen(owner, container.Owner, true) != openable)
                    continue;

                var d = Vector2.Distance(ownerMap.Position, _transform.GetMapCoordinates(container.Owner).Position);

                if (d < bestDist)
                {
                    best = container.Owner;
                    bestDist = d;
                }

                continue;
            }

            if (openable)
                continue;

            var hostileMap = _transform.GetMapCoordinates(hostile);

            if (hostileMap.MapId != ownerMap.MapId)
                continue;

            if (_interaction.InRangeUnobstructed(ownerMap, hostileMap, range + 1f, predicate: e => e == owner || e == hostile))
                continue;

            var start = ownerMap.Position;
            var segment = hostileMap.Position - start;
            var lengthSq = segment.LengthSquared();

            if (lengthSq < 0.01f)
                continue;

            _storages.Clear();
            _lookup.GetEntitiesInRange(ownerMap, range, _storages);

            foreach (var storage in _storages)
            {
                var pos = _transform.GetMapCoordinates(storage.Owner).Position;
                var along = Vector2.Dot(pos - start, segment) / lengthSq;

                if (along <= 0f || along >= 1f)
                    continue;

                if (Vector2.Distance(start + segment * along, pos) > lineWidth)
                    continue;

                var d = Vector2.Distance(start, pos);

                if (d >= bestDist)
                    continue;

                var blocker = storage.Owner;

                if (!_interaction.InRangeUnobstructed(ownerMap, hostileMap, range + 1f,
                        predicate: e => e == owner || e == hostile || e == blocker))
                {
                    continue;
                }

                best = blocker;
                bestDist = d;
            }
        }

        return best;
    }
}
