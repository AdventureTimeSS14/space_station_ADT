using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem
{
    private void InitializeHarmAbilities()
    {
        SubscribeLocalEvent<PhantomComponent, BreakdownActionEvent>(OnBreakdown);
        SubscribeLocalEvent<PhantomComponent, BloodBlindingActionEvent>(OnBlinding);
        SubscribeLocalEvent<PhantomComponent, PhantomControlActionEvent>(OnControl);
        SubscribeLocalEvent<PhantomComponent, GhostInjuryActionEvent>(OnGhostInjury);
    }

    private void OnBreakdown(EntityUid uid, PhantomComponent component, BreakdownActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, args, target))
            return;

        var time = TimeSpan.FromSeconds(2);
        var timeStatic = TimeSpan.FromSeconds(15);
        var timeHaunted = TimeSpan.FromHours(1);
        var chance = 0.2f;
        bool success = false;

        if (IsHolder(target, component))
        {
            if (TryComp<StatusEffectsComponent>(target, out var status) && HasComp<BatteryComponent>(target) && _status.TryAddStatusEffect<SeeingStaticComponent>(target, "SeeingStatic", timeHaunted, true, status))
            {
                if (!component.BreakdownOn)
                {
                    UpdateEctoplasmSpawn(uid);
                    _sharedStun.TryParalyze(target, timeHaunted, true);
                    _status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", timeHaunted, false, status);
                }
                else
                {
                    args.Handled = true;

                    _status.TryRemoveStatusEffect(target, "SlowedDown");
                    _status.TryRemoveStatusEffect(target, "SeeingStatic");
                }
                component.BreakdownOn = !component.BreakdownOn;
            }

            if (HasComp<MindShieldComponent>(target))
            {
                if (_random.Prob(chance))
                {
                    UpdateEctoplasmSpawn(uid);
                    var stunTime = TimeSpan.FromSeconds(1);
                    RemComp<MindShieldComponent>(target);
                    _sharedStun.TryParalyze(target, stunTime, true);

                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-target"), target, target, PopupType.MediumCaution);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-target"), target, target, PopupType.SmallCaution);
                }
                args.Handled = true;
            }
        }
        else
        {
            if (component.BreakdownOn)
            {
                var selfMessageActive = Loc.GetString("phantom-breakdown-fail-active");
                _popup.PopupEntity(selfMessageActive, uid, uid);
                return;
            }

            if (TryComp<PoweredLightComponent>(target, out var light))
            {
                _poweredLight.TryDestroyBulb(target, light);
                success = true;
            }

            if (TryComp<ApcComponent>(target, out var apc))
            {
                _apcSystem.ApcToggleBreaker(target, apc);
                success = true;
            }

            if (HasComp<StatusEffectsComponent>(target) && HasComp<BatteryComponent>(target) && _status.TryAddStatusEffect<SeeingStaticComponent>(target, "SeeingStatic", timeStatic, false))
            {
                _sharedStun.TryParalyze(target, time, false);
                _status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", timeStatic, false);
                success = true;
            }

            if (HasComp<MindShieldComponent>(target))
            {
                if (_random.Prob(chance))
                {
                    var stunTime = TimeSpan.FromSeconds(4);
                    RemComp<MindShieldComponent>(target);
                    _sharedStun.TryParalyze(target, stunTime, true);
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-target"), target, target, PopupType.MediumCaution);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
                    _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-target"), target, target, PopupType.SmallCaution);
                }
                success = true;
            }

            if (success)
            {
                UpdateEctoplasmSpawn(uid);
                args.Handled = true;
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/breakdown.ogg"), target);
            }
            else
            {
                var selfMessage = Loc.GetString("phantom-breakdown-fail");
                _popup.PopupEntity(selfMessage, uid, uid);
            }
        }
    }

    private void OnBlinding(EntityUid uid, PhantomComponent component, BloodBlindingActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, args, target))
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
            !TryComp<StatusEffectsComponent>(target, out var status) ||
            !TryComp<BlindableComponent>(target, out var blindable) ||
            HasComp<SiliconComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-blinding-noblood");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-mindshield", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (_playerManager.TryGetSessionByEntity(uid, out var targetSession))
            _audio.PlayGlobal(args.Sound, targetSession);

        _bloodstreamSystem.TryModifyBleedAmount(target, component.BlindingBleed, bloodstream);
        _status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, "TemporaryBlindness", component.BlindingTime, true);
        _blindable.AdjustEyeDamage((target, blindable), 3);
        var targetMessage = Loc.GetString("phantom-blinding-target");
        var message = Loc.GetString("phantom-blinding-others", ("name", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(targetMessage, target, target, PopupType.LargeCaution);
        _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
    }

    private void OnControl(EntityUid uid, PhantomComponent component, PhantomControlActionEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        var target = component.Holder;

        if (!TryUseAbility(uid, args, target))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!HasComp<VesselComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-puppeter-fail-notvessel", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-mindshield", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        args.Handled = true;

        if (_playerManager.TryGetSessionByEntity(uid, out var targetSession))
            _audio.PlayGlobal(args.Sound, targetSession);

        UpdateEctoplasmSpawn(uid);
        _controlled.TryStartControlling(uid, target, 30f, 10, "Phantom");
    }

    private void OnGhostInjury(EntityUid uid, PhantomComponent component, GhostInjuryActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, args, null))
            return;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        foreach (var mob in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 12f))
        {
            if (HasComp<VesselComponent>(mob) ||
                !HasComp<HumanoidAppearanceComponent>(mob) ||
                !CheckProtection(uid, mob))
                continue;

            if (_playerManager.TryGetSessionByEntity(mob, out var targetSession))
                _audio.PlayGlobal(args.Sound, targetSession);

            var stunTime = TimeSpan.FromSeconds(2);
            var list = _proto.EnumeratePrototypes<DamageGroupPrototype>().ToList();

            var damage = new DamageSpecifier(_random.Pick(list), component.InjuryDamage);
            _damageableSystem.TryChangeDamage(mob, damage);
            _sharedStun.TryParalyze(mob, stunTime, true);
        }
    }
}
