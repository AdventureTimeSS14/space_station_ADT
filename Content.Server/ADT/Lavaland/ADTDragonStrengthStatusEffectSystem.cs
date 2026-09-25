using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTDragonStrengthStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
        {
            TryMessage(target, comp.WarMessages, comp.WarChance, PopupType.SmallCaution);
            return;
        }

        var multiplier = MathF.Min(comp.MaxMultiplier, ((total - comp.MultiplierOffset) / comp.MultiplierScale).Float() + 1f);

        foreach (var (group, amount) in comp.Healing)
        {
            _damageable.HealDistributed((target, damageable), -amount * multiplier, group);
        }

        TryMessage(target, comp.HopeMessages, comp.HopeChance, PopupType.Small);
    }

    private void TryMessage(EntityUid target, List<LocId> messages, float chance, PopupType type)
    {
        if (messages.Count == 0 || !_random.Prob(chance))
            return;

        _popup.PopupEntity(Loc.GetString(_random.Pick(messages)), target, target, type);
    }
}
