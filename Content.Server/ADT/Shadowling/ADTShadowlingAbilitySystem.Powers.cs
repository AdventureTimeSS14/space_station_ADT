using Content.Shared.ADT.Shadowling;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Stunnable;
using Content.Shared.Chemistry.Components;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private void InitializePowers()
    {
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingGlareEvent>(OnGlare);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingVeilEvent>(OnVeil);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingShadowWalkEvent>(OnShadowWalk);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingIcyVeinsEvent>(OnIcyVeins);

        SubscribeLocalEvent<ADTLesserShadowlingComponent, ADTShadowlingGlareEvent>(OnLesserGlare);
        SubscribeLocalEvent<ADTLesserShadowlingComponent, ADTShadowlingShadowWalkEvent>(OnLesserShadowWalk);
    }

    private void OnGlare(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingGlareEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        args.Handled = DoGlare(ent, args.Target, args.Action);
    }

    private void OnLesserGlare(Entity<ADTLesserShadowlingComponent> ent, ref ADTShadowlingGlareEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = DoGlare(ent, args.Target, args.Action);
    }

    private bool DoGlare(EntityUid user, EntityUid target, EntityUid action)
    {
        if (!TryComp<ADTShadowlingGlareActionComponent>(action, out var glare))
            return false;

        if (IsHiveMember(target) || !HasComp<MobStateComponent>(target) || _mobState.IsIncapacitated(target))
            return false;

        _popup.PopupEntity(Loc.GetString("shadowling-glare-cast", ("user", user)), user, PopupType.MediumCaution);
        _audio.PlayPvs(glare.Sound, user);

        var distance = (_transform.GetWorldPosition(target) - _transform.GetWorldPosition(user)).Length();

        if (distance <= glare.MeleeRange)
        {
            _stun.TryKnockdown(target, glare.CloseKnockdown, true);
            _status.TryAddStatusEffect<MutedComponent>(target, glare.MuteEffect, glare.CloseMute, true);
            _stamina.TryTakeStamina(target, glare.CloseStamina, source: user);
            _popup.PopupEntity(Loc.GetString("shadowling-glare-close"), target, target, PopupType.LargeCaution);
            return true;
        }

        _stun.TryAddParalyzeDuration(target, glare.FarStun);
        _status.TryAddStatusEffect<StunnedStatusEffectComponent>(target, glare.SlowEffect, glare.FarSlow, false);
        _status.TryAddStatusEffect<MutedComponent>(target, glare.MuteEffect, glare.FarMute, true);
        _popup.PopupEntity(Loc.GetString("shadowling-glare-far"), target, target, PopupType.LargeCaution);
        return true;
    }

    private void OnVeil(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingVeilEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingVeilActionComponent>(args.Action, out var veil))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-veil-cast"), ent, ent);
        _audio.PlayPvs(veil.Sound, ent);

        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, veil.Range))
        {
            ExtinguishLight(nearby);
        }

        args.Handled = true;
    }

    private void ExtinguishLight(EntityUid uid)
    {
        if (TryComp<PoweredLightComponent>(uid, out var powered))
        {
            _poweredLight.SetState(uid, false, powered);
            return;
        }

        if (HasComp<SlimPoweredLightComponent>(uid))
        {
            _slimLight.SetEnabled(uid, false);
            return;
        }

        if (TryComp<HandheldLightComponent>(uid, out var handheld) && handheld.Activated)
            _handheldLight.TurnOff((uid, handheld));
    }

    private void OnShadowWalk(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingShadowWalkEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        args.Handled = DoShadowWalk(ent, args.Action);
    }

    private void OnLesserShadowWalk(Entity<ADTLesserShadowlingComponent> ent, ref ADTShadowlingShadowWalkEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = DoShadowWalk(ent, args.Action);
    }

    private bool DoShadowWalk(EntityUid user, EntityUid action)
    {
        if (!TryComp<ADTShadowlingShadowWalkActionComponent>(action, out var walk))
            return false;

        var coords = Transform(user).Coordinates;

        if (_polymorph.PolymorphEntity(user, walk.Polymorph) is not { } shadow)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-shadow-walk-failed"), user, user);
            return false;
        }

        _audio.PlayPvs(walk.Sound, coords);
        _popup.PopupEntity(Loc.GetString("shadowling-shadow-walk-enter", ("user", shadow)), shadow, PopupType.Medium);

        if (walk.EnterEffect is { } effect)
            Spawn(effect, coords);

        return true;
    }

    private void OnIcyVeins(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingIcyVeinsEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingIcyVeinsActionComponent>(args.Action, out var icy))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-icy-veins-cast"), ent, ent);
        _audio.PlayPvs(icy.Sound, ent);

        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, icy.Range))
        {
            if (!HasComp<MobStateComponent>(nearby) || _mobState.IsDead(nearby))
                continue;

            if (IsHiveMember(nearby))
            {
                if (nearby != ent.Owner)
                    _popup.PopupEntity(Loc.GetString("shadowling-icy-veins-immune"), nearby, nearby);

                continue;
            }

            _popup.PopupEntity(Loc.GetString("shadowling-icy-veins-hit"), nearby, nearby, PopupType.LargeCaution);
            _stun.TryAddParalyzeDuration(nearby, icy.Stun);
            _damageable.TryChangeDamage(nearby, icy.Damage, true, origin: ent.Owner);

            if (icy.ReagentAmount > 0)
            {
                var solution = new Solution();
                solution.AddReagent(icy.Reagent, icy.ReagentAmount);
                _bloodstream.TryAddToBloodstream(nearby, solution);
            }
        }

        args.Handled = true;
    }
}
