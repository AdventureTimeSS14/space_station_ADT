using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class HereticFireHeatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    private static readonly ProtoId<DamageTypePrototype> HeatDamageType = "Heat";
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextRefresh;
    private DamageTypePrototype _heatDamage = default!;

    public override void Initialize()
    {
        base.Initialize();

        _heatDamage = _proto.Index(HeatDamageType);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now >= _nextRefresh)
        {
            _nextRefresh = now + RefreshInterval;

            var fires = EntityQueryEnumerator<HereticAshFireComponent, TransformComponent>();
            while (fires.MoveNext(out var fire, out var fireXform))
            {
                var coords = _xform.GetMapCoordinates((fire, fireXform));

                foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.45f))
                {
                    if (mob.Owner == fire)
                        continue;

                    var heat = EnsureComp<HereticFireHeatComponent>(mob);
                    heat.Expires = now + Lifetime;

                    if (heat.NextTick == TimeSpan.Zero)
                        heat.NextTick = now;

                    Dirty(mob, heat);
                }
            }
        }

        var heated = EntityQueryEnumerator<HereticFireHeatComponent, DamageableComponent>();
        while (heated.MoveNext(out var uid, out var heat, out _))
        {
            if (now >= heat.Expires)
            {
                RemCompDeferred<HereticFireHeatComponent>(uid);
                continue;
            }

            if (now < heat.NextTick)
                continue;

            heat.NextTick = now + heat.Interval;
            Dirty(uid, heat);

            var spec = new DamageSpecifier(_heatDamage, heat.DamagePerTick);
            _damage.TryChangeDamage(uid, spec, ignoreResistances: true);
        }
    }
}
