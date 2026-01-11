using Content.Shared.Actions;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.Buckle.Components;

namespace Content.Shared.ADT.BarbellBench.Systems;

public abstract class SharedBarbellBenchSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public const string BarbellRepActionId = "ActionBarbellBenchPerformRep";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BarbellBenchComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<BarbellBenchComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnMapInit(Entity<BarbellBenchComponent> ent, ref MapInitEvent args)
    {
        // Создаем действие для этой скамьи при инициализации
        _actConts.EnsureAction(ent.Owner, ref ent.Comp.BarbellRepAction, BarbellRepActionId);
        Dirty(ent);
    }

    private void OnStrapped(Entity<BarbellBenchComponent> bench, ref StrappedEvent args)
    {
        // Когда игрок ложится на скамью, даем ему действие
        _actionsSystem.AddAction(args.Buckle, ref bench.Comp.BarbellRepAction, BarbellRepActionId, bench);
        Dirty(bench);
    }

    private void OnUnstrapped(Entity<BarbellBenchComponent> bench, ref UnstrappedEvent args)
    {
        // Когда игрок встает, убираем действие
        _actionsSystem.RemoveAction(args.Buckle.Owner, bench.Comp.BarbellRepAction);
    }
}
