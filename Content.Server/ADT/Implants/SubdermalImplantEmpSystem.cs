using Content.Shared.ADT.BerserkImplant;
using Content.Shared.ADT.BloodPumpImplant;
using Content.Shared.ADT.Implants;
using Content.Shared.ADT.KereznikovImplant;
using Content.Shared.ADT.KvasImplant;
using Content.Shared.ADT.SecondHeartImplant;
using Content.Shared.ADT.SpasezhnikovImplant;
using Content.Shared.ADT.StimulantImplant;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.ADT.Implants;

public sealed class SubdermalImplantEmpSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodPumpImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnBloodPumpEmp);
        SubscribeLocalEvent<StimulantImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnStimulantEmp);
        SubscribeLocalEvent<SecondHeartImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnSecondHeartEmp);
        SubscribeLocalEvent<KereznikovImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnKereznikovEmp);
        SubscribeLocalEvent<SpasezhnikovImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnSpasezhnikovEmp);
        SubscribeLocalEvent<KvasImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnKvasEmp);
        SubscribeLocalEvent<BerserkImplantComponent, ImplantRelayEvent<EmpPulseEvent>>(OnBerserkEmp);
    }

    private void OnBloodPumpEmp(Entity<BloodPumpImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args) =>
        ApplyEmp(ent.Owner);

    private void OnStimulantEmp(Entity<StimulantImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args) =>
        ApplyEmp(ent.Owner);

    private void OnSecondHeartEmp(Entity<SecondHeartImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args) =>
        ApplyEmp(ent.Owner);

    private void OnKereznikovEmp(Entity<KereznikovImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args)
    {
        ApplyEmp(ent.Owner);

        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var subComp) || subComp.ImplantedEntity is not { } mob)
            return;

        if (HasComp<KereznikovActiveComponent>(mob))
        {
            _speed.RefreshMovementSpeedModifiers(mob);
            RemCompDeferred<KereznikovActiveComponent>(mob);
        }
    }

    private void OnSpasezhnikovEmp(Entity<SpasezhnikovImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args) =>
        ApplyEmp(ent.Owner);

    private void OnKvasEmp(Entity<KvasImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args) =>
        ApplyEmp(ent.Owner);

    private void OnBerserkEmp(Entity<BerserkImplantComponent> ent, ref ImplantRelayEvent<EmpPulseEvent> args)
    {
        ApplyEmp(ent.Owner);

        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var subComp) || subComp.ImplantedEntity is not { } mob)
            return;

        RemCompDeferred<BerserkImplantActiveComponent>(mob);
    }

    private void ApplyEmp(EntityUid implantUid)
    {
        if (!TryComp<SubdermalImplantComponent>(implantUid, out var subComp) || subComp.ImplantedEntity is not { } mob)
            return;

        if (!TryComp<SubdermalImplantEmpComponent>(implantUid, out var empComp))
            return;

        _damageable.TryChangeDamage(mob, empComp.EmpDamage, ignoreResistances: true);
        Spawn("EffectSparks", Transform(mob).Coordinates);

        if (subComp.Action is { } action)
            _actions.SetCooldown(action, TimeSpan.FromSeconds(empComp.EmpCooldown));
    }
}
