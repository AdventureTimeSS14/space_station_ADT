using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.ADT.Vampire;

public sealed class VampirePoolSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampirePoolComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<VampirePoolComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<VampirePoolComponent> ent, ref StartCollideEvent args)
    {
        if (!IsBloodPuddle(ent.Comp, args.OtherEntity) || !ent.Comp.Puddles.Add(args.OtherEntity))
            return;

        UpdateHidden(ent);
    }

    private void OnEndCollide(Entity<VampirePoolComponent> ent, ref EndCollideEvent args)
    {
        if (!ent.Comp.Puddles.Remove(args.OtherEntity))
            return;

        UpdateHidden(ent);
    }

    private void UpdateHidden(Entity<VampirePoolComponent> ent)
    {
        var hidden = ent.Comp.Puddles.Count > 0;
        if (ent.Comp.Hidden == hidden)
            return;

        ent.Comp.Hidden = hidden;
        Dirty(ent);
    }

    private bool IsBloodPuddle(VampirePoolComponent pool, EntityUid uid)
    {
        if (!TryComp<PuddleComponent>(uid, out var puddle) ||
            !_solution.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var solution))
        {
            return false;
        }

        foreach (var reagent in pool.BloodReagents)
        {
            if (solution.GetTotalPrototypeQuantity(reagent) > 0)
                return true;
        }

        return false;
    }
}
