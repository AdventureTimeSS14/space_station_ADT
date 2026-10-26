using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
namespace Content.Shared.Damage.Systems;
public sealed class HealOnHoldingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [ValidatePrototypeId<EntityPrototype>]
    private const string HealEffect = "ADTEffectHealPlusTripleYellow";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealOnHoldingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HealOnHoldingComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }
    private void OnMapInit(EntityUid uid, HealOnHoldingComponent component, MapInitEvent args)
    {
        component.NextHeal = _timing.CurTime;
    }
    private void OnRemovedFromContainer(EntityUid uid, HealOnHoldingComponent component, EntRemovedFromContainerMessage args)
    {
        component.NextHeal = _timing.CurTime + TimeSpan.FromSeconds(component.Interval);
    }

    private EntityUid? GetMobOwner(EntityUid uid)
    {
        var current = uid;
        var xform = Transform(current);
        while (xform.ParentUid.IsValid())
        {
            current = xform.ParentUid;
            if (HasComp<MobStateComponent>(current))
                return current;

            xform = Transform(current);
        }
        return null;
    }
    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;

        var blockedQuery = EntityQueryEnumerator<HealOnHoldingBlockedComponent>();
        while (blockedQuery.MoveNext(out var blockedUid, out var blocked))
        {
            if (blocked.Expires <= curTime)
                RemComp<HealOnHoldingBlockedComponent>(blockedUid);
        }
        var query = EntityQueryEnumerator<HealOnHoldingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.NextHeal > curTime)
                continue;
            var mob = GetMobOwner(uid);
            if (mob == null)
                continue;
            if (!TryComp<DamageableComponent>(mob.Value, out var damageable))
                continue;
            if (damageable.TotalDamage <= FixedPoint2.Zero)
                continue;

            if (HasComp<HealOnHoldingBlockedComponent>(mob.Value))
                continue;

            var block = EnsureComp<HealOnHoldingBlockedComponent>(mob.Value);
            block.Expires = curTime + TimeSpan.FromSeconds(component.Interval);

            var effect = Spawn(HealEffect, new EntityCoordinates(mob.Value, Vector2.Zero));
            _transform.SetParent(effect, mob.Value);

            var healDamage = component.Damage.Clone();
            foreach (var type in healDamage.DamageDict.Keys)
            {
                healDamage.DamageDict[type] = -FixedPoint2.Abs(healDamage.DamageDict[type]);
            }
            _damageableSystem.TryChangeDamage((mob.Value, damageable), healDamage, true, false, uid);
            component.NextHeal = curTime + TimeSpan.FromSeconds(component.Interval);
        }
    }
}