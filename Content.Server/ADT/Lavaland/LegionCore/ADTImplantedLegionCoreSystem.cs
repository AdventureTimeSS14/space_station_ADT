using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.ADT.Lavaland.LegionCore;

public sealed class ADTImplantedLegionCoreSystem : EntitySystem
{
    [Dependency] private readonly ADTLegionCoreToleranceSystem _tolerance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTImplantedLegionCoreComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ADTImplantedLegionCoreComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != ent.Comp.TriggerState)
            return;

        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return;

        var comp = ent.Comp;
        RemComp<ADTImplantedLegionCoreComponent>(ent);

        var cost = _tolerance.TakeCellularCost(ent.Owner, comp.CellularMultiplier);
        var repair = BuildRepair((ent.Owner, damageable), comp, cost);

        if (!repair.Empty)
            _damageable.TryChangeDamage(ent.Owner, repair, true, false);

        InjectAdrenaline(ent.Owner, comp);

        if (_mobState.IsIncapacitated(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-trigger-fail"), ent.Owner, ent.Owner, PopupType.LargeCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-trigger"), ent.Owner, ent.Owner, PopupType.Medium);
    }

    private DamageSpecifier BuildRepair(
        Entity<DamageableComponent> target,
        ADTImplantedLegionCoreComponent comp,
        FixedPoint2 cellularCost)
    {
        var repair = new DamageSpecifier();

        if (cellularCost > FixedPoint2.Zero)
            repair.DamageDict[ADTLegionCoreToleranceSystem.CellularDamage] = cellularCost;

        var budget = FixedPoint2.New(_random.NextFloat(comp.HealMin.Float(), comp.HealMax.Float())) + cellularCost;
        var healable = FixedPoint2.Zero;

        foreach (var (type, amount) in target.Comp.Damage.DamageDict)
        {
            if (type == ADTLegionCoreToleranceSystem.CellularDamage || amount <= FixedPoint2.Zero)
                continue;

            healable += amount;
        }

        if (healable <= FixedPoint2.Zero)
            return repair;

        var scale = Math.Min(1f, budget.Float() / healable.Float());

        foreach (var (type, amount) in target.Comp.Damage.DamageDict)
        {
            if (type == ADTLegionCoreToleranceSystem.CellularDamage || amount <= FixedPoint2.Zero)
                continue;

            repair.DamageDict[type] = amount * -scale;
        }

        return repair;
    }

    private void InjectAdrenaline(EntityUid target, ADTImplantedLegionCoreComponent comp)
    {
        if (comp.AdrenalineAmount <= FixedPoint2.Zero)
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        var amount = comp.AdrenalineAmount;

        if (_solutionContainer.TryGetSolution(target, bloodstream.BloodSolutionName, out _, out var blood))
        {
            var current = blood.GetTotalPrototypeQuantity(comp.Adrenaline);
            amount = FixedPoint2.Min(amount, comp.AdrenalineMaxLevel - current);
        }

        if (amount <= FixedPoint2.Zero)
            return;

        _bloodstream.TryAddToBloodstream((target, bloodstream), new Solution(comp.Adrenaline, amount));
    }
}
