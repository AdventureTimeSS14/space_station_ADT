using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTDragonStrengthStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDragonStrengthStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<ADTDragonStrengthStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<ADTDragonStrengthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Target = args.Target;
        ent.Comp.NextTick = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnRemoved(Entity<ADTDragonStrengthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Target = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDragonStrengthStatusEffectComponent>();

        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Target is not { } target || TerminatingOrDeleted(target))
                continue;

            if (comp.NextTick > curTime)
                continue;

            comp.NextTick = curTime + comp.Interval;
            TryHeal(target, comp);
        }
    }

    private void TryHeal(EntityUid target, ADTDragonStrengthStatusEffectComponent comp)
    {
        if (_mobState.IsDead(target) || !TryComp<DamageableComponent>(target, out var damageable))
            return;

        var total = _damageable.GetTotalDamage((target, damageable));

        if (total < comp.DamageThreshold)
            return;

        var multiplier = MathF.Min(comp.MaxMultiplier, ((total - comp.MultiplierOffset) / comp.MultiplierScale).Float() + 1f);
        var healing = new DamageSpecifier();

        foreach (var (group, amount) in comp.Healing)
        {
            healing += new DamageSpecifier(_proto.Index(group), -amount * multiplier);
        }

        _damageable.TryChangeDamage((target, damageable), healing, true, false);
    }
}
