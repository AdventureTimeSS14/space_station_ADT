using System.Collections.Generic;
using Content.Shared.ADT.ClothesBite;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.ADT.ClothesBite;

/// <summary>
/// Lets an attacker (mothroach) feed off a struck mob's worn, digestible clothing.
/// </summary>
public sealed class ClothesBiteOnHitSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothesBiteOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ClothesBiteOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!_body.TryGetOrgansWithComponent<StomachComponent>(ent.Owner, out var stomachs))
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner)
                continue;

            BiteWornClothing(ent, stomachs, target);
        }
    }

    private void BiteWornClothing(Entity<ClothesBiteOnHitComponent> ent, List<Entity<StomachComponent>> stomachs, EntityUid target)
    {
        // Only actually-worn clothing, not pocket contents.
        if (!_inventory.TryGetContainerSlotEnumerator(target, out var enumerator, SlotFlags.WITHOUT_POCKET))
            return;

        var edible = new List<EntityUid>();
        while (enumerator.NextItem(out var item, out _))
        {
            if (CanBite(item, stomachs))
                edible.Add(item);
        }

        if (edible.Count == 0)
            return;

        Bite(ent, stomachs, _random.Pick(edible));
    }

    private bool CanBite(EntityUid item, List<Entity<StomachComponent>> stomachs)
    {
        if (!TryComp<EdibleComponent>(item, out var edible))
            return false;

        if (!_solutionContainer.TryGetSolution(item, edible.Solution, out _, out var solution) || solution.Volume <= 0)
            return false;

        return _ingestion.IsDigestibleBy(item, stomachs, out _);
    }

    private void Bite(Entity<ClothesBiteOnHitComponent> ent, List<Entity<StomachComponent>> stomachs, EntityUid item)
    {
        if (!TryComp<EdibleComponent>(item, out var edible))
            return;

        if (!_solutionContainer.TryGetSolution(item, edible.Solution, out var soln, out var solution) || solution.Volume <= 0)
            return;

        Entity<StomachComponent>? stomach = null;
        foreach (var candidate in stomachs)
        {
            if (_ingestion.IsDigestibleBy(item, candidate))
            {
                stomach = candidate;
                break;
            }
        }

        if (stomach == null)
            return;

        var amount = FixedPoint2.Min(ent.Comp.Amount, solution.Volume);
        var split = _solutionContainer.SplitSolution(soln.Value, amount);
        _stomach.TryTransferSolution(stomach.Value.Owner, split, stomach.Value.Comp);

        // Drained dry: finish it off like a full eat (fires FullyEatenEvent, then deletes).
        if (solution.Volume <= 0 && edible.DestroyOnEmpty)
        {
            var finishedEv = new FullyEatenEvent(ent.Owner);
            RaiseLocalEvent(item, ref finishedEv);
            QueueDel(item);
        }
    }
}
