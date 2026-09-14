//

using Content.Server.Heretic.Components.PathSpecific;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Heretic;
using Content.Shared.CombatMode.Pacification;
using Robust.Shared.Timing;
using Content.Shared.Heretic.Components.PathSpecific;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly BladeArenaSystem _arena = default!;

    protected override void SubscribeBlade()
    {
        base.SubscribeBlade();

        SubscribeLocalEvent<EventHereticRealignment>(OnRealignment);
        SubscribeLocalEvent<HereticChampionStanceEvent>(OnChampionStance);
        SubscribeLocalEvent<EventHereticFuriousSteel>(OnFuriousSteel);
        SubscribeLocalEvent<EventHereticSacraments>(OnSacraments);
        SubscribeLocalEvent<EventHereticDomainExpansion>(OnDomainExpansion);
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
        EnsureComp<ChampionStanceComponent>(args.Heretic);

        var riposte = EnsureComp<RiposteeComponent>(args.Heretic);
        if (!riposte.Data.TryGetValue("HereticBlade", out var data))
        {
            data = new RiposteData();
            riposte.Data["HereticBlade"] = data;
        }

        data.Cooldown = Math.Min(data.Cooldown, 10f);
        data.Timer = Math.Min(data.Timer, 10f);
        Dirty(args.Heretic, riposte);
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

    private void OnSacraments(EventHereticSacraments args)
    {
        if (!TryUseAbility(args))
            return;

        _statusNew.TryAddStatusEffectDuration(args.Performer, args.Status, args.Time);
        args.Handled = true;
    }

    private void OnDomainExpansion(EventHereticDomainExpansion args)
    {
        var uid = args.Performer;

        if (!TryUseAbility(args, false) || !Heretic.TryGetHereticComponent(uid, out var heretic, out _))
            return;

        var coords = Transform(uid).Coordinates;
        var victims = Lookup.GetEntitiesInRange<MobStateComponent>(coords, args.Radius);

        foreach (var victim in victims)
        {
            if (victim.Owner == uid || Heretic.IsHereticOrGhoul(victim.Owner))
                continue;

            if (victim.Comp.CurrentState != MobState.Alive)
                continue;

            _pulling.TryStartPull(uid, victim.Owner, force: true);

            var mark = EnsureComp<HereticCombatMarkComponent>(victim.Owner);
            mark.DisappearTime = mark.MaxDisappearTime;
            mark.Path = "Blade";
            mark.Repetitions = 1;
            Dirty(victim.Owner, mark);

            _stam.TakeStaminaDamage(victim.Owner, 25f);
        }

        _arena.TrySpawnArena(coords, "HereticArena", "PlatingRoseStone", 3, (int) args.Radius);

        args.Handled = true;
    }
}
