using System.Linq;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class HereticFireHeatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    private ProtoId<TagPrototype> FireTag = "HereticAshFire";
    private TimeSpan Lifetime = TimeSpan.FromSeconds(1.5);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var fires = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (fires.MoveNext(out var fire, out var tag, out var fireXform))
        {
            if (!_tag.HasTag(tag, FireTag))
                continue;

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

            var spec = new DamageSpecifier();
            spec.DamageDict.Add(_proto.Index<DamageTypePrototype>("Heat"), heat.DamagePerTick);
            _damage.TryChangeDamage(uid, spec, ignoreResistances: true);
        }
    }
}