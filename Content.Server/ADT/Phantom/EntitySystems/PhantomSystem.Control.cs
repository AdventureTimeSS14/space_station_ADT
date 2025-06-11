using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem
{
    private void InitializeControlAbilities()
    {
        SubscribeLocalEvent<PhantomComponent, StarvationActionEvent>(OnStarvation);
        SubscribeLocalEvent<PhantomComponent, ParalysisActionEvent>(OnParalysis);
        SubscribeLocalEvent<PhantomComponent, PhantomOathActionEvent>(OnOath);
        SubscribeLocalEvent<PhantomComponent, PsychoEpidemicActionEvent>(OnPsychoEpidemic);

        SubscribeLocalEvent<PhantomComponent, PuppeterDoAfterEvent>(PuppeterDoAfter);
    }

    private void OnStarvation(EntityUid uid, PhantomComponent component, StarvationActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, target))
            return;

        if (IsHolder(target, component))
        {
            if (!component.StarvationOn)
            {
                UpdateEctoplasmSpawn(uid);
                var timeHaunted = TimeSpan.FromHours(1);
                _status.TryAddStatusEffect<HungerEffectComponent>(target, "ADTStarvation", timeHaunted, false);
                if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                    _audio.PlayGlobal(component.InjurySound, mind.Session);
            }
            else
            {
                args.Handled = true;

                _status.TryRemoveStatusEffect(target, "ADTStarvation");
            }

            component.StarvationOn = !component.StarvationOn;
        }
        else
        {
            if (component.StarvationOn)
            {
                var selfMessage = Loc.GetString("phantom-starvation-fail-active");
                _popup.PopupEntity(selfMessage, uid, uid);
                return;
            }
            else
            {
                args.Handled = true;

                UpdateEctoplasmSpawn(uid);
                var time = TimeSpan.FromSeconds(15);
                _status.TryAddStatusEffect<HungerEffectComponent>(target, "ADTStarvation", time, false);
                if (_playerManager.TryGetSessionByEntity(uid, out var session))
                    _audio.PlayGlobal(component.InjurySound, session);
            }
        }
    }

    private void OnParalysis(EntityUid uid, PhantomComponent component, ParalysisActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, args, target))
            return;

        if (IsHolder(target, component))
        {
            if (!component.ParalysisOn)
            {
                UpdateEctoplasmSpawn(uid);
                var timeHaunted = TimeSpan.FromHours(1);
                _sharedStun.TryParalyze(target, timeHaunted, false);
            }
            else
            {
                _status.TryRemoveStatusEffect(target, "KnockedDown");
                _status.TryRemoveStatusEffect(target, "Stun");

                args.Handled = true;
            }
            component.ParalysisOn = !component.ParalysisOn;
        }
        else
        {
            if (component.ParalysisOn)
            {
                var selfMessage = Loc.GetString("phantom-paralysis-fail-active");
                _popup.PopupEntity(selfMessage, uid, uid);
            }
            else
            {
                UpdateEctoplasmSpawn(uid);
                var time = TimeSpan.FromSeconds(args.Duration);
                if (_sharedStun.TryParalyze(target, time, false))
                    args.Handled = true;
            }
        }
    }

    private void OnOath(EntityUid uid, PhantomComponent component, PhantomOathActionEvent args)
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
        if (HasComp<PhantomPuppetComponent>(target))
            return;

        args.Handled = true;
        var makeVesselDoAfter = new DoAfterArgs(EntityManager, uid, component.MakeVesselDuration, new PuppeterDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(makeVesselDoAfter);
    }

    private void OnPsychoEpidemic(EntityUid uid, PhantomComponent component, PsychoEpidemicActionEvent args)
    {
        if (args.Handled)
            return;

        List<EntityUid> list = new();
        foreach (var (ent, humanoid) in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, 150f))
        {
            if (
                _mindSystem.TryGetMind(ent, out _, out _) &&
                TryComp<MobStateComponent>(ent, out var state) &&
                state.CurrentState == Shared.Mobs.MobState.Alive &&
                !HasComp<MindShieldComponent>(ent))
                list.Add(ent);
        }

        if (list.Count <= 0)
        {
            var failMessage = Loc.GetString("phantom-epidemic-fail");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        var target = _random.Pick(list);

        if (!_hallucinations.StartEpidemicHallucinations(target, component.HallucinationsPrototype))
        {
            var failMessage = Loc.GetString("phantom-epidemic-fail");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
            _audio.PlayGlobal(component.PsychoSound, session);

        var selfMessage = Loc.GetString("phantom-epidemic-success", ("name", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessage, uid, uid);

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;
    }

    private void PuppeterDoAfter(EntityUid uid, PhantomComponent component, PuppeterDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        args.Handled = true;

        var target = args.Args.Target.Value;
        if (args.Cancelled)
        {
            var selfMessage = Loc.GetString("phantom-vessel-interrupted");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_playerManager.TryGetSessionByEntity(target, out var session))
            return;

        _euiManager.OpenEui(new AcceptPhantomPowersEui(target, this, component), session);
    }
}
