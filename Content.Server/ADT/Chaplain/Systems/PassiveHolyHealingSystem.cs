using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Components;

namespace Content.Server.ADT.Chaplain;

public sealed class PassiveHolyHealingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string HolyDamageType = "Holy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveHolyHealingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, PassiveHolyHealingComponent component, ComponentStartup args)
    {
        component.NextHealTime = _timing.CurTime + TimeSpan.FromSeconds(component.Interval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_prototype.HasIndex<DamageTypePrototype>(HolyDamageType))
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PassiveHolyHealingComponent, DamageableComponent>();

        while (query.MoveNext(out var uid, out var comp, out var damageable))
        {
            if (TerminatingOrDeleted(uid)
                || comp.NextHealTime > curTime
                || !TryComp<MobStateComponent>(uid, out var mobState))
                continue;

            if (_mobState.IsDead(uid, mobState))
                continue;

            comp.NextHealTime = curTime + TimeSpan.FromSeconds(comp.Interval);

            var healSpec = new DamageSpecifier();
            healSpec.DamageDict[HolyDamageType] = comp.HealAmount;

            _damageable.TryChangeDamage(uid, healSpec, true);
        }
    }
}