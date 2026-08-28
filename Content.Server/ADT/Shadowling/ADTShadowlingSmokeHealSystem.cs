using Content.Shared.ADT.Shadowling;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shadowling;

public sealed class ADTShadowlingSmokeHealSystem : EntitySystem
{
    [Dependency] private readonly ADTShadowlingAbilitySystem _shadowling = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShadowlingSmokeHealComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<ADTShadowlingSmokeHealComponent> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;

        if (TerminatingOrDeleted(target) || !_shadowling.IsHiveMember(target))
            return;

        if (HasComp<ADTShadowlingSmokeHealedComponent>(target))
            return;

        var healed = AddComp<ADTShadowlingSmokeHealedComponent>(target);
        healed.NextHeal = _timing.CurTime + ent.Comp.Interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ADTShadowlingSmokeHealedComponent>();
        while (query.MoveNext(out var uid, out var healed))
        {
            if (!TryComp<SmokeAffectedComponent>(uid, out var affected))
            {
                RemCompDeferred(uid, healed);
                continue;
            }

            if (!TryComp<ADTShadowlingSmokeHealComponent>(affected.SmokeEntity, out var smoke))
                continue;

            if (curTime < healed.NextHeal)
                continue;

            healed.NextHeal = curTime + smoke.Interval;
            _damageable.TryChangeDamage(uid, smoke.Heal, true);
        }
    }
}
