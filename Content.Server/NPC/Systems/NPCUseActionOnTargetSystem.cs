using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

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
        //ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
    }

    public bool TryUseAction(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        // if (!TryComp<EntityWorldTargetActionComponent>(user.Comp.ActionEnt, out var action))
        //     return false;
        var weights = _proto.Index(user.Comp.Actions);
        var act = weights.Pick();
        var actionEntity = user.Comp.ActionEntities.Keys.Where(x => Prototype(x)?.ID == act).First();

        if (!_actions.TryGetActionData(actionEntity, out var action))
            return false;

        if (!_actions.ValidAction(action))
            return false;

        switch (action.BaseEvent)
        {
            case InstantActionEvent instant:
                break;
            case EntityTargetActionEvent entityTarget:
                entityTarget.Target = target;
                break;
            case EntityWorldTargetActionEvent entityWorldTarget:
                entityWorldTarget.Entity = target;
                entityWorldTarget.Coords = Transform(target).Coordinates;
                break;
            case WorldTargetActionEvent worldTarget:
                worldTarget.Target = Transform(target).Coordinates;
                break;
        }

        _actions.PerformAction(user,
            null,
            actionEntity,
            action,
            action.BaseEvent,
            _timing.CurTime,
            false);

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

            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            TryUseAction((uid, comp), target);
        }
    }
}
