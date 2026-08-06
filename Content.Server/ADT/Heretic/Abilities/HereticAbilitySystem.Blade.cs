//

using Content.Server.Heretic.Components.PathSpecific;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Heretic;
using Content.Shared.CombatMode.Pacification;
using Robust.Shared.Timing;
using Content.Shared.Heretic.Components.PathSpecific;
using Content.Shared.Stunnable;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeBlade()
    {
        base.SubscribeBlade();

        SubscribeLocalEvent<EventHereticRealignment>(OnRealignment);
        SubscribeLocalEvent<HereticChampionStanceEvent>(OnChampionStance);
        SubscribeLocalEvent<EventHereticFuriousSteel>(OnFuriousSteel);
    }

    private void OnRealignment(EventHereticRealignment args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        RemCompDeferred<KnockedDownComponent>(ent);
        RemCompDeferred<StunnedComponent>(ent);

        _statusEffect.TryRemoveStatusEffect(ent, "ForcedSleep");
        _statusEffect.TryRemoveStatusEffect(ent, "Drowsiness");

        if (TryComp<StaminaComponent>(ent, out var stam))
        {
            if (stam.StaminaDamage >= stam.CritThreshold)
                _stam.ExitStamCrit(ent, stam);

            // ADT: no ToggleStaminaDrain, clear stamina directly
            _stam.TakeStaminaDamage(ent, -stam.StaminaDamage, stam);
            Dirty(ent, stam);
        }

        _standing.Stand(ent);
        _pulling.StopAllPulls(ent, stopPuller: false);
        if (_statusEffect.TryAddStatusEffect<PacifiedComponent>(ent, "Pacified", TimeSpan.FromSeconds(10f), true))
            _statusEffect.TryAddStatusEffect<RealignmentComponent>(ent, "Realignment", TimeSpan.FromSeconds(10f), true);

        args.Handled = true;
    }

    private void OnChampionStance(HereticChampionStanceEvent args)
    {
        // ADT: no limb dismemberment lock, no shitmed
    }

    private void OnFuriousSteel(EventHereticFuriousSteel args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        _pblade.AddProtectiveBlade(ent);
        for (var i = 1; i < 3; i++)
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.5f * i),
                () =>
                {
                    if (TerminatingOrDeleted(ent))
                        return;

                    _pblade.AddProtectiveBlade(ent);
                });
        }

        args.Handled = true;
    }
}
