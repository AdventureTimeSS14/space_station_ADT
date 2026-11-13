using Content.Shared.ADT.Cloning;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Cloning;

public sealed class ADTCloneCellularDecaySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCloneCellularDecayComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTCloneCellularDecayComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDamage = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTCloneCellularDecayComponent>();
        while (query.MoveNext(out var uid, out var decay))
        {
            if (curTime < decay.NextDamage)
                continue;

            decay.NextDamage = curTime + decay.Interval;

            if (_mobState.IsDead(uid))
                continue;

            _damageable.TryChangeDamage(uid, decay.Damage, ignoreResistances: true, interruptsDoAfters: false);
        }
    }
}
