//

using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Slippery;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stam = default!;

    protected virtual void SubscribeBlade()
    {
        // Protective blades prevent that
        // SubscribeLocalEvent<SilverMaelstromComponent, BeforeStaminaDamageEvent>(OnBeforeBladeStaminaDamage);
        // Still knocked down by a flashbang or something - it's fine
        // SubscribeLocalEvent<SilverMaelstromComponent, BeforeStatusEffectAddedEvent>(OnBeforeBladeStatusEffect);
        // Remove this after adding noslip heretic magboots side knowledge
        SubscribeLocalEvent<SilverMaelstromComponent, SlipAttemptEvent>(OnBladeSlipAttempt);
        // Protective blades do that
        // SubscribeLocalEvent<SilverMaelstromComponent, BeforeHarmfulActionEvent>(OnBladeHarmfulAction);

        SubscribeLocalEvent<RealignmentComponent, BeforeStaminaDamageEvent>(OnBeforeBladeStaminaDamage);
        SubscribeLocalEvent<RealignmentComponent, BeforeOldStatusEffectAddedEvent>(OnBeforeBladeStatusEffect);
        SubscribeLocalEvent<RealignmentComponent, SlipAttemptEvent>(OnBladeSlipAttempt);
        SubscribeLocalEvent<RealignmentComponent, BeforeHarmfulActionEvent>(OnBladeHarmfulAction);
        SubscribeLocalEvent<RealignmentComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RealignmentComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<HereticBladePassiveRiposteEvent>(OnBladePassiveRiposte);
    }

    private void OnBladePassiveRiposte(HereticBladePassiveRiposteEvent args)
    {
        var riposte = EnsureComp<RiposteeComponent>(args.Heretic);

        if (!riposte.Data.TryGetValue("HereticBlade", out var data))
        {
            data = new RiposteData();
            riposte.Data["HereticBlade"] = data;
        }

        data.Cooldown = Math.Min(data.Cooldown, args.Cooldown);
        data.Timer = Math.Min(data.Timer, args.Cooldown);
        Dirty(args.Heretic, riposte);
    }

    private void OnComponentRemove(Entity<RealignmentComponent> ent, ref ComponentRemove args)
    {
        // ADT: no Goob stamina-drain system, regen is server-side
    }

    private void OnStatusEnded(Entity<RealignmentComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != "Pacified")
            return;

        if (!Status.TryRemoveStatusEffect(ent, "Realignment"))
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnBladeHarmfulAction(EntityUid uid, Component component, BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || args.Type == HarmfulActionType.Harm)
            return;

        args.Cancel();
    }

    private void OnBladeSlipAttempt(EntityUid uid, Component component, SlipAttemptEvent args)
    {
        args.NoSlip = true;
    }

    private void OnBeforeBladeStatusEffect(EntityUid uid, Component component, ref BeforeOldStatusEffectAddedEvent args)
    {
        if (args.EffectKey is not ("KnockedDown" or "Stun"))
            return;

        args.Cancelled = true;
    }

    private void OnBeforeBladeStaminaDamage(EntityUid uid, Component component, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        args.Cancelled = true;
    }
}
