using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.ADT.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;

    protected virtual void SubscribeFlesh()
    {
        SubscribeLocalEvent<EventHereticFleshSurgery>(OnFleshSurgery);
        // ADT: instant cast now, kept for stale do-afters after hotreload
        SubscribeLocalEvent<EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);

        SubscribeLocalEvent<FleshPassiveComponent, ImmuneToPoisonDamageEvent>(OnPoisonImmune);

        // ADT: instant touch-spell, no DoAfter
        SubscribeLocalEvent<FleshSurgeryComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnPoisonImmune(Entity<FleshPassiveComponent> ent, ref ImmuneToPoisonDamageEvent args)
    {
        args.Immune = true;
    }

    private void OnAfterInteract(Entity<FleshSurgeryComponent> ent, ref AfterInteractEvent args)
    {
        // ADT: instant, no DoAfter; self-cast allowed (heals the heretic)
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;

        // ally = self, any heretic, or ghoul
        if (target == args.User || Heretic.IsHereticOrGhoul(target))
        {
            // 50 physical damage total (Blunt+Slash+Piercing)
            var heal = new DamageSpecifier();
            heal.DamageDict["Blunt"] = FixedPoint2.New(-20);
            heal.DamageDict["Slash"] = FixedPoint2.New(-20);
            heal.DamageDict["Piercing"] = FixedPoint2.New(-10);
            IHateWoundMed(target, heal, null, null, null, null, null);
        }
        else
        {
            // enemy: 50 blunt + near-starving
            var dmg = new DamageSpecifier();
            dmg.DamageDict["Blunt"] = FixedPoint2.New(50);
            _dmg.TryChangeDamage(target, dmg, origin: args.User);

            // hunger/thirst dropped near-critical, not instant death
            if (TryComp(target, out HungerComponent? hunger)
                && hunger.Thresholds.TryGetValue(HungerThreshold.Starving, out var starving))
                _hunger.SetHunger(target, starving, hunger);

            if (TryComp(target, out ThirstComponent? thirst)
                && thirst.ThirstThresholds.TryGetValue(ThirstThreshold.Parched, out var parched))
                _thirst.SetThirst(target, thirst, parched);
        }

        InvokeTouchSpell<FleshSurgeryComponent>((ent.Owner, ent.Comp), args.User);
    }

    private void OnFleshSurgery(EventHereticFleshSurgery args)
    {
        var touch = GetTouchSpell<EventHereticFleshSurgery, FleshSurgeryComponent>(args.Performer, ref args);
        if (touch == null)
            return;

        EnsureComp<FleshSurgeryComponent>(touch.Value).Action = args.Action.Owner;
    }

    private void OnFleshSurgeryDoAfter(EventHereticFleshSurgeryDoAfter args)
    {
        // ADT: kept for old do-afters, new logic is instant
        if (args.Cancelled || args.Target == null)
            return;

        if (!TryComp(args.Used, out FleshSurgeryComponent? surgery))
            return;

        InvokeTouchSpell<FleshSurgeryComponent>((args.Used.Value, surgery), args.User);
        IHateWoundMed(args.Target.Value, null, null, null, null, null, null);
        args.Handled = true;
    }
}
