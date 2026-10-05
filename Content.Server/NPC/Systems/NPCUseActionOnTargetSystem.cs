using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

// ADT: Система была полностью переписан, заменяйте при апстриме на нашу версию.

public sealed class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const float MaxActionRange = 20f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        var weights = _proto.Index(ent.Comp.Actions);
        foreach (var item in weights.Weights)
        {
            var actionEnt = _actions.AddAction(ent, item.Key);
            if (actionEnt.HasValue)
                ent.Comp.ActionEntities.Add(actionEnt.Value, item.Value);
        }
    }

    public bool TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        var weights = _proto.Index(user.Comp.Actions);
        var act = weights.Pick();
        var actionEntity = user.Comp.ActionEntities.Keys.Where(x => Prototype(x)?.ID == act).First();

        if (_actions.GetAction(actionEntity) is not { } action)
            return false;

        if (!_actions.ValidAction(action))
            return false;

        _actions.SetEventTarget(action, target);

        _actions.PerformAction(user.Owner, action, predicted: false);

        user.Comp.LastAction = _timing.CurTime;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (_timing.CurTime < comp.LastAction + TimeSpan.FromSeconds(comp.Delay))
                continue;

            if (TerminatingOrDeleted(uid) || _mobState.IsDead(uid))
                continue;

            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            if (!IsValidTarget(target) || GetDistance(uid, target) is not { } distance || distance > MaxActionRange)
            {
                htn.Blackboard.Remove<EntityUid>(comp.TargetKey);
                htn.Blackboard.Remove<EntityCoordinates>("TargetCoordinates");
                continue;
            }

            TryUseAction((uid, comp), target);
        }
    }

    private bool IsValidTarget(EntityUid target)
    {
        if (!target.IsValid() || TerminatingOrDeleted(target))
            return false;

        return !_mobState.IsDead(target);
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
