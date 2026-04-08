using Content.Server.Body.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Species.Drask;
public sealed class DraskColdHealingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DraskColdHealingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DraskColdHealingComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnMapInit(Entity<DraskColdHealingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextHealTime = _gameTiming.CurTime + ent.Comp.Interval;
    }

    private void OnUnpaused(Entity<DraskColdHealingComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextHealTime += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<DraskColdHealingComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (curTime < comp.NextHealTime)
                continue;

            comp.NextHealTime = curTime + comp.Interval;

            if (!TryComp<TemperatureComponent>(uid, out var tempComp))
                continue;

            if (!HasComp<DamageableComponent>(uid))
                continue;

            var currentTempCelsius = tempComp.CurrentTemperature - 295.15f;

            if (currentTempCelsius <= comp.TemperatureThreshold)
            {
                _damageable.ChangeDamage(uid, comp.Healing, true, false);
            }
        }
    }
}
