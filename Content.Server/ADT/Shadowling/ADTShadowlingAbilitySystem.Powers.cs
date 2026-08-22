using Content.Goobstation.Common.CCVar;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Stunnable;
using Content.Shared.Chemistry.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

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

        SubscribeLocalEvent<ADTShadowlingShadowFormComponent, PolymorphedEvent>(OnShadowFormPolymorphed);
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

        ExtinguishBlindingLights(ent.Owner, veil);

        args.Handled = true;
    }

    private void ExtinguishBlindingLights(EntityUid uid, ADTShadowlingVeilActionComponent veil)
    {
        var range = MathF.Max(veil.Range, _cfg.GetCVar(GoobCVars.LightDetectionRange));
        var xform = Transform(uid);
        var worldPos = _transform.GetWorldPosition(xform);

        foreach (var (light, pointLight) in _lookup.GetEntitiesInRange<PointLightComponent>(xform.Coordinates, range))
        {
            if (!pointLight.Enabled || light == uid)
                continue;

            var distance = (_transform.GetWorldPosition(light) - worldPos).Length();

            if (distance > pointLight.Radius)
                continue;

            if (!_examine.InRangeUnOccluded(light, uid, pointLight.Radius))
                continue;

            ExtinguishLight(light);
        }
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
        {
            _handheldLight.TurnOff((uid, handheld), false);
            return;
        }

        if (TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight) && flashlight.LightOn)
            _flashlight.SetLight((uid, flashlight), false, quiet: true);
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

        if (EnterShadowForm(user, walk.Polymorph) is not { } shadow)
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

    public EntityUid? EnterShadowForm(EntityUid user, ProtoId<PolymorphPrototype> polymorph)
    {
        var state = Factory.GetComponent<ADTShadowlingShadowFormComponent>();
        SaveCamera(user, state);
        SaveLanguage(user, state);

        if (_polymorph.PolymorphEntity(user, polymorph) is not { } form)
            return null;

        AddComp(form, state);
        RestoreCamera(form, state);
        RestoreLanguage(form, state);

        return form;
    }

    private void OnShadowFormPolymorphed(Entity<ADTShadowlingShadowFormComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        SaveCamera(ent.Owner, ent.Comp);
        SaveLanguage(ent.Owner, ent.Comp);

        RestoreCamera(args.NewEntity, ent.Comp);
        RestoreLanguage(args.NewEntity, ent.Comp);
    }

    private void SaveCamera(EntityUid uid, ADTShadowlingShadowFormComponent state)
    {
        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        state.RelativeEntity = mover.RelativeEntity;
        state.RelativeRotation = mover.RelativeRotation;
        state.TargetRelativeRotation = mover.TargetRelativeRotation;
    }

    private void RestoreCamera(EntityUid uid, ADTShadowlingShadowFormComponent state)
    {
        if (state.RelativeEntity is not { } relative || !TryComp<InputMoverComponent>(uid, out var mover))
            return;

        mover.RelativeEntity = relative;
        mover.RelativeRotation = state.RelativeRotation;
        mover.TargetRelativeRotation = state.TargetRelativeRotation;
        mover.LerpTarget = TimeSpan.Zero;
        Dirty(uid, mover);
    }

    private void SaveLanguage(EntityUid uid, ADTShadowlingShadowFormComponent state)
    {
        if (!TryComp<LanguageSpeakerComponent>(uid, out var speaker))
            return;

        state.Languages = new Dictionary<string, LanguageKnowledge>(speaker.Languages);
        state.CurrentLanguage = speaker.CurrentLanguage;
    }

    private void RestoreLanguage(EntityUid uid, ADTShadowlingShadowFormComponent state)
    {
        if (state.Languages == null || !TryComp<LanguageSpeakerComponent>(uid, out var speaker))
            return;

        foreach (var (language, knowledge) in state.Languages)
        {
            if (!speaker.Languages.ContainsKey(language))
                speaker.Languages.Add(language, knowledge);
        }

        if (state.CurrentLanguage != null && speaker.Languages.ContainsKey(state.CurrentLanguage))
            speaker.CurrentLanguage = state.CurrentLanguage;

        _language.UpdateUi(uid, speaker);
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
