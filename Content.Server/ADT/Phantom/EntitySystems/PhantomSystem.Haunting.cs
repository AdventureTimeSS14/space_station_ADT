using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem
{
    private void InitializeHaunting()
    {
        SubscribeLocalEvent<PhantomComponent, MakeHolderActionEvent>(OnMakeHolder);
        SubscribeLocalEvent<PhantomComponent, StopHauntingActionEvent>(OnStopHaunting);
        SubscribeLocalEvent<PhantomComponent, UpdateCanMoveEvent>(OnTryMove);
        SubscribeLocalEvent<PhantomComponent, StopHauntAlertEvent>(OnStopHauntAlertClick);

        SubscribeLocalEvent<PhantomHolderComponent, MobStateChangedEvent>(OnHauntedDeath);
        SubscribeLocalEvent<PhantomHolderComponent, EntityTerminatingEvent>(OnHauntedDeleted);
        SubscribeLocalEvent<PhantomHolderComponent, PolymorphedEvent>(OnHauntedPolymorphed);
        SubscribeLocalEvent<PhantomHolderComponent, EctoplasmHitscanHitEvent>(OnHauntedEctoplasmicDamage);
    }

    private void OnMakeHolder(EntityUid uid, PhantomComponent component, MakeHolderActionEvent args)
    {
        if (args.Handled)
            return;
        if (!component.CanHaunt)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, args, target))
            return;

        if (!component.HasHaunted)
            Haunt(uid, target);
        else
        {
            StopHaunt(uid, component.Holder, component);
            Haunt(uid, target);
        }

        args.Handled = true;
    }

    private void OnStopHaunting(EntityUid uid, PhantomComponent component, StopHauntingActionEvent args)
    {
        if (args.Handled)
            return;
        if (!component.CanHaunt)
            return;

        StopHaunt(uid, component.Holder, component);

        args.Handled = true;
    }

    private void OnTryMove(EntityUid uid, PhantomComponent component, UpdateCanMoveEvent args)
    {
        if (!component.HasHaunted)
            return;

        args.Cancel();
    }

    private void OnStopHauntAlertClick(EntityUid uid, PhantomComponent component, ref StopHauntAlertEvent args)
    {
        StopHaunt(uid, component.Holder, component);
    }

    private void OnHauntedDeath(EntityUid uid, PhantomHolderComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<PhantomComponent>(component.Phantom, out var comp))
            return;

        if (comp.TyranyStarted && comp.Vessels.Count <= 0)
            ChangeEssenceAmount(component.Phantom, -1000, allowDeath: true);
    }

    private void OnHauntedDeleted(EntityUid uid, PhantomHolderComponent component, EntityTerminatingEvent args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;

        StopHaunt(component.Phantom, uid, phantom);
    }

    private void OnHauntedPolymorphed(EntityUid uid, PhantomHolderComponent component, PolymorphedEvent args)
    {
        StopHaunt(component.Phantom, uid);

        if (HasComp<HumanoidAppearanceComponent>(args.NewEntity))
            Haunt(component.Phantom, args.NewEntity);
    }

    private void OnHauntedEctoplasmicDamage(EntityUid uid, PhantomHolderComponent component, EctoplasmHitscanHitEvent args)
    {
        _damageableSystem.TryChangeDamage(component.Phantom, args.DamageToPhantom);
        _damageableSystem.TryChangeDamage(uid, args.DamageToTarget, true);
        StopHaunt(component.Phantom, uid);
    }

    #region Utility
    /// <summary>
    /// Checks is phantom able to haunt target
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="target">Target uid</param>
    /// <returns>Is phantom able to haunt target</returns>
    private bool TryHaunt(EntityUid uid, EntityUid target)
    {
        if (HasComp<PhantomHolderComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-haunt-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("phantom-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Haunt target
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    public void Haunt(EntityUid uid, EntityUid target, bool silent = false, PhantomComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.CanHaunt)
            return;

        if (!TryHaunt(uid, target))
            return;

        UpdateEctoplasmSpawn(uid);
        var targetXform = Transform(target);
        while (targetXform.ParentUid.IsValid())
        {
            if (targetXform.ParentUid == uid)
                return;

            targetXform = Transform(targetXform.ParentUid);
        }

        if (component.Holder == target)
            return;

        component.HasHaunted = true;
        component.Holder = target;
        var holderComp = EnsureComp<PhantomHolderComponent>(target);
        holderComp.Phantom = uid;
        _actionBlocker.UpdateCanMove(uid);

        _appearance.SetData(uid, PhantomVisuals.Haunting, true);

        var xform = Transform(uid);
        _container.AttachParentToContainerOrGrid((uid, xform));

        // If we didn't get to parent's container.
        if (xform.ParentUid != Transform(xform.ParentUid).ParentUid)
        {
            _transform.SetCoordinates(uid, xform, new EntityCoordinates(target, Vector2.Zero), rotation: Angle.Zero);
        }

        if (component.IsCorporeal)
        {
            if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
            {
                var fixture = fixtures.Fixtures.First();

                _physicsSystem.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.GhostImpassable, fixtures);
                _physicsSystem.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);
            }
            var visibility = EnsureComp<VisibilityComponent>(uid);

            _visibility.SetLayer((uid, visibility), (int)VisibilityFlags.Ghost, false);
            _visibility.RefreshVisibility(uid);

            component.IsCorporeal = false;
        }

        _physicsSystem.SetLinearVelocity(uid, Vector2.Zero);

        _alerts.ShowAlert(uid, component.HauntedAlert);

        if (!silent)
        {
            var selfMessage = Loc.GetString("phantom-haunt-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("phantom-haunt-target");
            _popup.PopupEntity(targetMessage, target, target);

            if (_playerManager.TryGetSessionByEntity(uid, out var session))
                _audio.PlayGlobal(new SoundCollectionSpecifier("PhantomHaunt"), session);
        }

        Dirty(target, holderComp);
    }

    /// <summary>
    /// Stops haunting target
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="holder">Haunted uid</param>
    /// <param name="component">Phantom component</param>
    public void StopHaunt(EntityUid uid, EntityUid holder, PhantomComponent? component = null)
    {
        if (!TryComp<PhantomHolderComponent>(holder, out var holderComp))
            return;
        if (!Resolve(uid, ref component))
            return;
        if (!component.CanHaunt)
            return;

        RemComp<PhantomHolderComponent>(holder);
        HauntedStopEffects(component.Holder, component);
        component.Holder = EntityUid.Invalid;
        component.HasHaunted = false;
        _actionBlocker.UpdateCanMove(uid);

        _alerts.ClearAlert(uid, component.HauntedAlert);
        //_action.RemoveAction(uid, component.PhantomStopHauntActionEntity);

        // var eye = EnsureComp<EyeComponent>(uid);
        // _eye.SetDrawFov(uid, false, eye);
        _appearance.SetData(uid, PhantomVisuals.Haunting, false);

        Dirty(holder, holderComp);

        if (!TryComp(uid, out TransformComponent? xform))
            return;

        _transform.AttachToGridOrMap(uid, xform);
        if (xform.MapUid != null)
            return;
    }

    /// <summary>
    /// Stopping all effects for haunted
    /// </summary>
    /// <param name="haunted">Haunted uid</param>
    /// <param name="component">Phantom component</param>
    public void HauntedStopEffects(EntityUid haunted, PhantomComponent component)
    {
        if (component.ParalysisOn)
        {
            _status.TryRemoveStatusEffect(haunted, "KnockedDown");
            _status.TryRemoveStatusEffect(haunted, "Stun");
            component.ParalysisOn = false;
        }
        if (component.BreakdownOn)
        {
            _status.TryRemoveStatusEffect(haunted, "SlowedDown");
            _status.TryRemoveStatusEffect(haunted, "SeeingStatic");
            component.BreakdownOn = false;
        }
        if (component.StarvationOn)
        {
            _status.TryRemoveStatusEffect(haunted, "ADTStarvation");
            component.StarvationOn = false;
        }
        if (component.ClawsOn)
        {
            QueueDel(component.Claws);
            component.Claws = new();
            component.ClawsOn = false;
        }
    }

    /// <summary>
    /// Checks if target is haunted
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="component">Phantom component</param>
    /// <returns>Is target holder</returns>
    public bool IsHolder(EntityUid target, PhantomComponent component)
    {
        if (target == component.Holder)
            return true;

        return false;
    }
    #endregion
}
