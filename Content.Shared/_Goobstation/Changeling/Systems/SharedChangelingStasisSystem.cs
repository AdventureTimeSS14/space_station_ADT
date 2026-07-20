using Content.Goobstation.Common.Changeling;
using Content.Goobstation.Shared.Changeling.Actions;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.Devour.Events;
using Content.Shared.Actions;
using Content.Shared.Body; // ADT-Tweak
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Utility;
using Content.Shared.Body.Systems; // ADT-Tweak
using Content.Shared.Body.Components; // ADT-Tweak

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChangelingStasisSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly MobThresholdSystem _mob = default!;
    [Dependency] private readonly MobStateSystem _state = default!;
    [Dependency] private readonly PullingSystem _pull = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private EntityQuery<AbsorbedComponent> _absorbQuery;
    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BloodstreamComponent> _bloodQuery;
    private EntityQuery<DamageableComponent> _dmgQuery;
    private EntityQuery<MindContainerComponent> _mindQuery;
    private EntityQuery<MobThresholdsComponent> _thresholdQuery;
    private EntityQuery<PullableComponent> _pullQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingStasisComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChangelingStasisComponent, ChangelingStasisEvent>(OnStasisAction);

        SubscribeLocalEvent<ChangelingStasisComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<ChangelingStasisComponent, TargetBeforeDefibrillatorZapsEvent>(OnDefibZap);
        SubscribeLocalEvent<ChangelingStasisComponent, CritSuccumbEvent>(OnSuccumb);
        SubscribeLocalEvent<ChangelingStasisComponent, CritLastWordsEvent>(OnLastWords);

        _absorbQuery = GetEntityQuery<AbsorbedComponent>();
        _bloodQuery = GetEntityQuery<BloodstreamComponent>();
        _bodyQuery = GetEntityQuery<BodyComponent>();
        _dmgQuery = GetEntityQuery<DamageableComponent>();
        _mindQuery = GetEntityQuery<MindContainerComponent>();
        _thresholdQuery = GetEntityQuery<MobThresholdsComponent>();
        _pullQuery = GetEntityQuery<PullableComponent>();
    }


    private void OnMapInit(Entity<ChangelingStasisComponent> ent, ref MapInitEvent args)
    {
        SetStasisTime(ent);

        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ChangelingStasisComponent> ent, ref ComponentShutdown args)
    {
        SetPreventGhosting(ent, false);

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    #region Event Handlers
    private void OnStasisAction(Entity<ChangelingStasisComponent> ent, ref ChangelingStasisEvent args)
    {
        if (!ent.Comp.IsInStasis)
            EnterStasis(ent);
        else
            ExitStasis(ent);

        args.Handled = true;
    }

    private void OnMobStateChange(Entity<ChangelingStasisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead
            && !ent.Comp.IsInStasis)
            EnterStasis(ent);
    }

    private void OnDefibZap(Entity<ChangelingStasisComponent> ent, ref TargetBeforeDefibrillatorZapsEvent args)
    {
        if (!ent.Comp.IsInStasis)
            return;

        DoPopup(ent, ent.Comp.ExitDefibPopup);
        ExitStasis(ent, false);
    }

    // prevents you from cheesing the no-ghost mechanic
    private void OnSuccumb(Entity<ChangelingStasisComponent> ent, ref CritSuccumbEvent args)
    {
        args.Handled = true;
    }

    private void OnLastWords(Entity<ChangelingStasisComponent> ent, ref CritLastWordsEvent args)
    {
        args.Handled = true;
    }

    #endregion

    #region Helper Methods
    private void EnterStasis(Entity<ChangelingStasisComponent> ent)
    {
        if (ent.Comp.IsInStasis
            || !_dmgQuery.TryComp(ent, out var dmgComp))
            return;

        if (_absorbQuery.HasComp(ent))
        {
            DoPopup(ent, ent.Comp.AbsorbedPopup);
            return;
        }

        var dmgType = ent.Comp.CritDamageProto;

        if (_state.IsAlive(ent)) // fake suicide message
        {
            dmgType = ent.Comp.AliveDamageProto;

            var popup = Loc.GetString(ent.Comp.EnterAlivePopup, ("name", ent));
            DoPopupOthers(ent, popup);
        }

        SetStasisTime(ent);

        // determine the popup to send
        if (ent.Comp.StasisTime >= ent.Comp.DeadStasisTime)
            DoPopup(ent, ent.Comp.EnterDeadPopup);

        else if (ent.Comp.StasisTime >= ent.Comp.CritStasisTime)
            DoPopup(ent, ent.Comp.EnterDamagedPopup);

        else
            DoPopup(ent, ent.Comp.EnterPopup);

        ent.Comp.IsInStasis = true;

        _suicide.ApplyLethalDamage((ent, dmgComp), dmgType); // kills thyself

        SetPreventGhosting(ent, true); // prevent ghosting

        // set the action cooldown and change toggle state
        _actions.SetToggled(ent.Comp.ActionEnt, true);
        _actions.SetUseDelay(ent.Comp.ActionEnt, ent.Comp.StasisTime);
        _actions.StartUseDelay(ent.Comp.ActionEnt);

        Dirty(ent);
    }

    private void ExitStasis(Entity<ChangelingStasisComponent> ent, bool heal = true)
    {
        if (!ent.Comp.IsInStasis
            || !_dmgQuery.TryComp(ent, out var dmgComp))
            return;

        if (_absorbQuery.HasComp(ent))
        {
            DoPopup(ent, ent.Comp.AbsorbedPopup);

            SetPreventGhosting(ent, false); // fallback. you're actually out of the game so you can ghost
            return;
        }

        // check if we're allowed to revive
        var reviveEv = new BeforeSelfRevivalEvent(ent, ent.Comp.SelfReviveFailPopup);
        RaiseLocalEvent(ent, ref reviveEv);

        if (reviveEv.Cancelled)
            return;

        if (_pullQuery.TryComp(ent, out var pullComp)
           && _pull.IsPulled(ent, pullComp))
        {
            var puller = pullComp.Puller;

            if (puller != null)
                // ADT-Tweak
                _stun.TryUpdateStunDuration(puller.Value, ent.Comp.StunTime);
        }

        if (heal)
            RestoreChangeling(ent);

        SetPreventGhosting(ent, false); // allow ghosting

        DoPopup(ent, ent.Comp.ExitPopup);

        ent.Comp.IsInStasis = false;

        // change action cooldown and toggle state back
        _actions.SetToggled(ent.Comp.ActionEnt, false);
        _actions.SetUseDelay(ent.Comp.ActionEnt, TimeSpan.FromSeconds(0));

        Dirty(ent);
    }

    private void SetStasisTime(Entity<ChangelingStasisComponent> ent)
    {
        if (!_dmgQuery.TryComp(ent, out var dmgComp)
            || !_mob.TryGetThresholdForState(ent, MobState.Dead, out var deadT)
            || !_mob.TryGetThresholdForState(ent, MobState.Critical, out var critT))
            return;

        var damage = dmgComp.TotalDamage;

        if (damage >= deadT)
        {
            ent.Comp.StasisTime = ent.Comp.DeadStasisTime;
            return;
        }

        if (damage >= critT)
        {
            ent.Comp.StasisTime = ent.Comp.CritStasisTime;
            return;
        }

        var scaled = damage / (critT ?? deadT) * ent.Comp.CritStasisTime.TotalSeconds;

        var time = MathF.Max((float) ent.Comp.DefaultStasisTime.TotalSeconds, (float) scaled);

        ent.Comp.StasisTime = TimeSpan.FromSeconds(time);
    }

    private void RestoreChangeling(Entity<ChangelingStasisComponent> ent)
    {
        // this recovers the changeling from all damage, refills blood, resets temperature and extinguishes fire
        // BUT does not restore limbs, purge chems or cure diseases.

        if (_thresholdQuery.TryComp(ent, out var threshComp)
            && _dmgQuery.TryComp(ent, out var dmgComp))
        {
            // taken straight from damageable rejuvenate method
            _mob.SetAllowRevives(ent, true, threshComp);
            _dmg.SetAllDamage(ent, dmgComp, 0);
            _mob.SetAllowRevives(ent, false, threshComp);
        }

        // ADT-Tweak

        // fix bleeding again (i :heart: woundmed)
        if (_bloodQuery.TryComp(ent, out var bloodComp))
        {
            _blood.TryModifyBleedAmount((ent, bloodComp), -bloodComp.BleedAmount);
            _blood.TryModifyBloodLevel((ent, bloodComp), bloodComp.BloodReferenceSolution.Volume);
        }

        ResetTemperature(ent);
        ExtinguishFire(ent);
    }

    protected virtual void ResetTemperature(Entity<ChangelingStasisComponent> ent)
    {
        // go to ChangelingStasisSystem for logic
    }

    protected virtual void ExtinguishFire(Entity<ChangelingStasisComponent> ent)
    {
        // go to ChangelingStasisSystem for logic
    }

    private void SetPreventGhosting(Entity<ChangelingStasisComponent> ent, bool state)
    {
        if (!_mind.TryGetMind(ent, out _, out var mind))
            return;

        mind.PreventGhosting = state;
    }

    private void DoPopup(Entity<ChangelingStasisComponent> ent, LocId popup)
    {
        _popup.PopupClient(Loc.GetString(popup), ent, ent);
    }

    private void DoPopupOthers(Entity<ChangelingStasisComponent> ent, string popup)
    {
        _popup.PopupPredicted(popup, ent, ent);
    }
    #endregion
}
