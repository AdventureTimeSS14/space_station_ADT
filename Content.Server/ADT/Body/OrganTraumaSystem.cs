using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed partial class OrganTraumaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";
    private static readonly ProtoId<OrganCategoryPrototype> Lungs = "Lungs";
    private static readonly ProtoId<OrganCategoryPrototype> Liver = "Liver";
    private static readonly ProtoId<OrganCategoryPrototype> Kidneys = "Kidneys";
    private static readonly ProtoId<OrganCategoryPrototype> Ears = "Ears";
    private static readonly ProtoId<OrganCategoryPrototype> Brain = "Brain";

    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly ProtoId<DamageTypePrototype> Radiation = "Radiation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganTraumaComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<OrganHealthComponent, BodyRelayedEvent<InhaledGasEvent>>(OnGasInhaled);
    }

    private void OnElectrocuted(Entity<OrganTraumaComponent> ent, ref ElectrocutedEvent args)
    {
        if (HasComp<DefibrillatorComponent>(args.SourceUid) || !CanTakeTrauma(ent))
            return;

        _organHealth.ChangeDamage(ent, Heart, ent.Comp.ElectrocutionHeartDamage * args.SiemensCoefficient, OrganKind.Organic);
    }

    private void OnGasInhaled(Entity<OrganHealthComponent> ent, ref BodyRelayedEvent<InhaledGasEvent> args)
    {
        if (ent.Comp.Kind != OrganKind.Organic
            || !HasComp<LungComponent>(ent)
            || CompOrNull<OrganComponent>(ent)?.Body is not { } body
            || !TryComp<OrganTraumaComponent>(body, out var trauma))
            return;

        var gas = args.Args.Gas;
        if (gas.TotalMoles < trauma.MinBreathMoles)
            return;

        if (gas.Temperature >= trauma.HotBreathTemperature || gas.Temperature <= trauma.ColdBreathTemperature)
            _organHealth.ChangeDamage((ent.Owner, ent.Comp), trauma.BreathBurnDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<OrganTraumaComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var trauma, out var damageable))
        {
            if (now < trauma.NextUpdate)
                continue;

            trauma.NextUpdate = now + trauma.UpdateInterval;

            if (!CanTakeTrauma(uid))
                continue;

            UpdateSustainedDamage((uid, trauma), _damageable.GetAllDamage((uid, damageable)));
            UpdateBarotrauma((uid, trauma));
        }
    }

    private void UpdateSustainedDamage(Entity<OrganTraumaComponent> ent, DamageSpecifier damage)
    {
        if (GetPositive(damage, Asphyxiation) >= ent.Comp.HypoxiaThreshold)
            _organHealth.ChangeDamage(ent, Brain, ent.Comp.HypoxiaBrainDamage, OrganKind.Organic);

        if (GetPositive(damage, Poison) >= ent.Comp.PoisonThreshold)
        {
            _organHealth.ChangeDamage(ent, Liver, ent.Comp.PoisonOrganDamage, OrganKind.Organic);
            _organHealth.ChangeDamage(ent, Kidneys, ent.Comp.PoisonOrganDamage, OrganKind.Organic);
        }

        if (GetPositive(damage, Radiation) >= ent.Comp.RadiationThreshold)
            _organHealth.ChangeDamage(ent, _random.Pick(ent.Comp.RadiationOrgans), ent.Comp.RadiationOrganDamage, OrganKind.Organic);
    }

    public void UpdateBarotrauma(Entity<OrganTraumaComponent> ent)
    {
        if (!TryComp<BarotraumaComponent>(ent, out var barotrauma) || !barotrauma.TakingDamage)
            return;

        if (_organHealth.TryGetOrgan(ent, Lungs, out var lungs))
        {
            var remaining = lungs.Comp.MaxDamage * ent.Comp.BarotraumaLungsMaxDamage - lungs.Comp.Damage;
            if (remaining > 0f)
                _organHealth.ChangeDamage((lungs.Owner, lungs.Comp), Math.Min(remaining, ent.Comp.BarotraumaLungsDamage), OrganKind.Organic);
        }

        _organHealth.ChangeDamage(ent, Ears, ent.Comp.BarotraumaEarsDamage, OrganKind.Organic);
    }

    private bool CanTakeTrauma(EntityUid body)
    {
        return !_mobState.IsDead(body) && !HasComp<ZombieComponent>(body);
    }

    private static FixedPoint2 GetPositive(DamageSpecifier damage, ProtoId<DamageTypePrototype> type)
    {
        return damage.DamageDict.TryGetValue(type, out var value) && value > FixedPoint2.Zero
            ? value
            : FixedPoint2.Zero;
    }
}
