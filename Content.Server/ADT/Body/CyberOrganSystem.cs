using Content.Server.Body.Systems;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed class CyberOrganSystem : EntitySystem
{
    private static readonly TimeSpan DecayStep = TimeSpan.FromSeconds(1);

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;
    [Dependency] private VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, EmpPulseEvent>(OnBodyEmpPulse);
        SubscribeLocalEvent<CyberOrganComponent, BodyRelayedEvent<EmpPulseEvent>>(OnEmpPulse);
    }

    private void OnBodyEmpPulse(Entity<BodyComponent> ent, ref EmpPulseEvent args)
    {
        _body.RelayEvent(ent, ref args);
    }

    private void OnEmpPulse(Entity<CyberOrganComponent> ent, ref BodyRelayedEvent<EmpPulseEvent> args)
    {
        var body = args.Body.Owner;
        var comp = ent.Comp;

        if (_timing.CurTime >= comp.NextEffect)
        {
            comp.NextEffect = _timing.CurTime + comp.EffectCooldown;

            if (comp.EmpDamage != null)
                _damageable.TryChangeDamage(body, comp.EmpDamage, ignoreResistances: true, interruptsDoAfters: false);

            if (comp.EmpJitter > TimeSpan.Zero)
                _jittering.DoJitter(body, comp.EmpJitter, true);

            if (comp.EmpVomit)
                _vomit.Vomit(body);
        }

        if (comp.EmpOrganDamage > 0f && _random.Prob(comp.EmpOrganDamageChance))
            _organHealth.ChangeDamage(ent.Owner, comp.EmpOrganDamage);

        if (!comp.Decaying && _random.Prob(comp.EmpFailureChance))
            comp.Decaying = true;

        var ev = args.Args;
        ev.Affected = true;
        args.Args = ev;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberOrganComponent, OrganHealthComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var cyber, out var health, out var organ))
        {
            if (!cyber.Decaying || organ.Body == null || _timing.CurTime < cyber.NextDecay)
                continue;

            cyber.NextDecay = _timing.CurTime + DecayStep;
            _organHealth.ChangeDamage((uid, health), health.MaxDamage * (float) (DecayStep / cyber.DecayDuration));
        }
    }
}
