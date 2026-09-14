using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private const string TargetKey = "Target";
    private const string TargetCoordinatesKey = "TargetCoordinates";
    private const string AggroVisionRadiusKey = "AggroVisionRadius";

    private const int AttackerSearchDepth = 5;

    private static readonly TimeSpan AggroUpdateInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextAggroUpdate;

    private void InitializeAggro()
    {
        SubscribeLocalEvent<MegafaunaComponent, DamageChangedEvent>(OnMegafaunaDamaged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextAggroUpdate)
            return;

        _nextAggroUpdate = now + AggroUpdateInterval;

        var query = EntityQueryEnumerator<MegafaunaComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (comp.Aggressor is not { } aggressor)
                continue;

            var ent = new Entity<MegafaunaComponent>(uid, comp);

            if (_mobState.IsDead(uid) || HasComp<ActorComponent>(uid))
            {
                ClearAggro(ent, htn);
                continue;
            }

            if (TerminatingOrDeleted(aggressor)
                || _mobState.IsDead(aggressor)
                || now > comp.AggroEndTime
                || GetDistance(uid, aggressor) == null)
            {
                ClearAggro(ent, htn);
                continue;
            }

            HoldTarget(ent, htn, aggressor);
        }
    }

    private void OnMegafaunaDamaged(Entity<MegafaunaComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.Origin is not { } origin)
            return;

        if (ResolveAttacker(origin) is not { } attacker)
            return;

        TryAggro(ent, attacker);
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

    public bool TryAggro(Entity<MegafaunaComponent> ent, EntityUid target)
    {
        if (target == ent.Owner)
            return false;

        if (!HasComp<MobStateComponent>(target))
            return false;

        if (_mobState.IsDead(target) || _mobState.IsDead(ent.Owner))
            return false;

        if (HasComp<ActorComponent>(ent.Owner))
            return false;

        if (_faction.IsEntityFriendly(ent.Owner, target))
            return false;

        if (GetDistance(ent.Owner, target) == null)
            return false;

        _faction.AggroEntity(ent.Owner, target);

        ent.Comp.Aggressor = target;
        ent.Comp.AggroEndTime = _timing.CurTime + ent.Comp.AggroMemory;

        if (!TryComp<HTNComponent>(ent.Owner, out var htn))
            return true;

        _npc.WakeNPC(ent.Owner, htn);
        HoldTarget(ent, htn, target);

        return true;
    }

    private void HoldTarget(Entity<MegafaunaComponent> ent, HTNComponent htn, EntityUid target)
    {
        if (GetDistance(ent.Owner, target) is not { } distance)
            return;

        var blackboard = htn.Blackboard;

        ent.Comp.BaseAggroRadius ??= blackboard.GetValueOrDefault<float>(AggroVisionRadiusKey, EntityManager);

        var wanted = MathF.Min(distance + ent.Comp.AggroRadiusPadding, ent.Comp.MaxAggroRadius);
        blackboard.SetValue(AggroVisionRadiusKey, MathF.Max(ent.Comp.BaseAggroRadius.Value, wanted));

        if (HasLiveTarget(blackboard))
            return;

        blackboard.SetValue(TargetKey, target);
        blackboard.SetValue(TargetCoordinatesKey, new EntityCoordinates(target, Vector2.Zero));
    }

    private void ClearAggro(Entity<MegafaunaComponent> ent, HTNComponent htn)
    {
        if (ent.Comp.Aggressor is { } aggressor)
            _faction.DeAggroEntity(ent.Owner, aggressor);

        ent.Comp.Aggressor = null;

        if (ent.Comp.BaseAggroRadius is { } baseRadius)
            htn.Blackboard.SetValue(AggroVisionRadiusKey, baseRadius);
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
}
