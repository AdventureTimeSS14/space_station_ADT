using Content.Server.ADT.Generation;
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
using Content.Shared.Mobs.Components;
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

    private readonly List<EntityUid> _pending = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTImplantedLegionCoreComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTImplantedLegionCoreComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Triggered)
                continue;

            comp.Triggered = false;
            _pending.Add(uid);
        }

        foreach (var uid in _pending)
        {
            Trigger(uid);
        }

        _pending.Clear();
    }

    private void OnMobStateChanged(Entity<ADTImplantedLegionCoreComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != ent.Comp.TriggerState || ent.Comp.Triggered)
            return;

        if (ent.Comp.LavalandOnly &&
            (Transform(ent.Owner).MapUid is not { } map || !HasComp<ADTLavalandGenerationComponent>(map)))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-weakened"), ent.Owner, ent.Owner, PopupType.LargeCaution);
            return;
        }

        ent.Comp.Triggered = true;
    }

    private void Trigger(EntityUid uid)
    {
        if (!TryComp<ADTImplantedLegionCoreComponent>(uid, out var comp) ||
            !TryComp<DamageableComponent>(uid, out var damageable) ||
            !TryComp<MobStateComponent>(uid, out var state) ||
            state.CurrentState != comp.TriggerState)
        {
            return;
        }

        RemComp<ADTImplantedLegionCoreComponent>(uid);

        var cost = _tolerance.TakeCellularCost(uid, comp.CellularMultiplier);
        var repair = BuildRepair((uid, damageable), comp, cost);

        if (!repair.Empty)
            _damageable.TryChangeDamage(uid, repair, true, false);

        _bloodstream.TryModifyBleedAmount(uid, -100f);
        InjectAdrenaline(uid, comp);

        if (_mobState.IsIncapacitated(uid))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-trigger-fail"), uid, uid, PopupType.LargeCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-trigger"), uid, uid, PopupType.Medium);
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
