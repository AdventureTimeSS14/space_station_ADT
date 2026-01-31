using Content.Shared.Actions;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.Buckle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.BarbellBench.Systems;

public abstract class SharedBarbellBenchSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] protected readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    public const string BarbellRepActionId = "ActionBarbellBenchPerformRep";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BarbellBenchComponent, StrappedEvent>(OnStrapped);
    }

    private void OnMapInit(Entity<BarbellBenchComponent> ent, ref MapInitEvent args)
    {
        _actConts.EnsureAction(ent.Owner, ref ent.Comp.BarbellRepAction, BarbellRepActionId);
        Dirty(ent);
    }

    protected virtual void OnStrapped(Entity<BarbellBenchComponent> bench, ref StrappedEvent args)
    {
        if (Container.TryGetContainer(bench.Owner, bench.Comp.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
        {
            _actionsSystem.AddAction(args.Buckle, ref bench.Comp.BarbellRepAction, BarbellRepActionId, bench);
            Dirty(bench);
        }
    }

    protected virtual void OnUnstrapped(Entity<BarbellBenchComponent> bench, ref UnstrappedEvent args)
    {
        if (bench.Comp.BarbellRepAction is { Valid: true } action)
        {
            // Unstrapped can happen even if the action was never actually granted (e.g. no barbell when strapped).
            // Only remove if this action is currently attached to the unbuckling entity.
            if (_actionsSystem.GetAction(action) is { } act && act.Comp.AttachedEntity == args.Buckle.Owner)
                _actionsSystem.RemoveProvidedAction(args.Buckle.Owner, bench.Owner, action);
        }

        if (bench.Comp.IsPerformingRep)
        {
            bench.Comp.IsPerformingRep = false;
            Dirty(bench);
            UpdateAppearance(bench.Owner, bench.Comp);
        }
    }

    protected void UpdateAppearance(EntityUid uid, BarbellBenchComponent component)
    {
        _appearance.SetData(uid, BarbellBenchVisuals.State,
            component.IsPerformingRep ? BarbellBenchState.PerformingRep : BarbellBenchState.Idle);
    }
}
