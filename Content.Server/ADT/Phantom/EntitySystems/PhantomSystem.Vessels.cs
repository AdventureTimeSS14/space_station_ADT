using System.Linq;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem
{
    private void InitializeVessels()
    {
        SubscribeLocalEvent<PhantomComponent, HauntVesselActionEvent>(OnRequestVesselMenu);
        SubscribeNetworkEvent<SelectPhantomVesselEvent>(OnSelectVessel);

        SubscribeLocalEvent<PhantomComponent, MakeVesselActionEvent>(OnMakeVessel);
        SubscribeLocalEvent<PhantomComponent, MakeVesselDoAfterEvent>(MakeVesselDoAfter);

        SubscribeLocalEvent<VesselComponent, MapInitEvent>(OnVesselInit);
        SubscribeLocalEvent<VesselComponent, ComponentShutdown>(OnVesselShutdown);
        SubscribeLocalEvent<VesselComponent, MobStateChangedEvent>(OnVesselDeath);
        SubscribeLocalEvent<VesselComponent, EntityTerminatingEvent>(OnVesselDeleted);
        SubscribeLocalEvent<VesselComponent, EctoplasmHitscanHitEvent>(OnVesselEctoplasmicDamage);

        SubscribeLocalEvent<PhantomPuppetComponent, MapInitEvent>(OnPupMapInit);
        SubscribeLocalEvent<PhantomPuppetComponent, ComponentShutdown>(OnPupShutdown);
        SubscribeLocalEvent<PhantomPuppetComponent, SelfGhostClawsActionEvent>(OnPupClaws);
        SubscribeLocalEvent<PhantomPuppetComponent, SelfGhostHealActionEvent>(OnPupHeal);

    }

    /// <summary>
    /// Requests radial menu for vessel haunting
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="args">Event</param>
    private void OnRequestVesselMenu(EntityUid uid, PhantomComponent component, HauntVesselActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.Vessels.Count <= 0)
        {
            var selfMessage = Loc.GetString("phantom-no-vessels");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        var ev = new RequestPhantomVesselMenuEvent(GetNetEntity(uid), new());

        foreach (var vessel in component.Vessels)
        {
            if (!TryComp<HumanoidAppearanceComponent>(vessel, out var humanoid))
                continue;

            var netEnt = GetNetEntity(vessel);

            // Here we build profiles for previews
            HumanoidCharacterAppearance hca = new();

            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.FacialHair, out var facialHair)
                && facialHair.TryGetValue(0, out var facialMarking))
            {
                hca = hca.WithFacialHairStyleName(facialMarking.MarkingId);
                hca = hca.WithFacialHairColor(facialMarking.MarkingColors.First());
            }
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Hair, out var hair)
                && hair.TryGetValue(0, out var hairMarking))
            {
                hca = hca.WithHairStyleName(hairMarking.MarkingId);
                hca = hca.WithHairColor(hairMarking.MarkingColors.First());
            }

            foreach (var item in Enum.GetValues<MarkingCategories>())
            {
                if (item is MarkingCategories.FacialHair or MarkingCategories.Hair)
                    continue;

                if (humanoid.MarkingSet.Markings.TryGetValue(item, out var mark))
                    hca = hca.WithMarkings(mark);
            }

            hca = hca.WithSkinColor(humanoid.SkinColor);
            hca = hca.WithEyeColor(humanoid.EyeColor);

            HumanoidCharacterProfile profile = new HumanoidCharacterProfile().WithCharacterAppearance(hca).WithSpecies(humanoid.Species);

            ev.Vessels.Add((netEnt, profile, Name(vessel)));
        }

        ev.Vessels.Sort();
        RaiseNetworkEvent(ev, session);

        args.Handled = true;
    }

    /// <summary>
    /// Raised when vessel selected
    /// </summary>
    /// <param name="args">Event</param>
    private void OnSelectVessel(SelectPhantomVesselEvent args)
    {
        var uid = GetEntity(args.Uid);
        var target = GetEntity(args.Vessel);
        if (!TryComp<PhantomComponent>(uid, out var comp))
            return;

        if (!HasComp<VesselComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-puppeter-fail-notvessel", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, target))
            return;

        if (!comp.HasHaunted)
            Haunt(uid, target);
        else
        {
            StopHaunt(uid, comp.Holder, comp);
            Haunt(uid, target);
        }
    }

    private void OnMakeVessel(EntityUid uid, PhantomComponent component, MakeVesselActionEvent args)
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

        if (!TryUseAbility(uid, target))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-mindshield", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<VesselComponent>(target))
        {
            RemComp<VesselComponent>(target);

            var selfMessage = Loc.GetString("phantom-vessel-removed", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_playerManager.TryGetSessionByEntity(target, out _))
        {
            var failMessage = Loc.GetString("phantom-no-mind");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        args.Handled = true;
        var makeVesselDoAfter = new DoAfterArgs(EntityManager, uid, component.MakeVesselDuration, new MakeVesselDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 15,
            BreakOnMove = false,
            BreakOnDamage = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(makeVesselDoAfter);
    }

    private void MakeVesselDoAfter(EntityUid uid, PhantomComponent component, MakeVesselDoAfterEvent args)
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

        if (component.Vessels.Contains(target))
            return;

        TryMakeVessel(uid, target, component);
    }

    #region Vessel event handlers
    private void OnVesselInit(EntityUid uid, VesselComponent component, MapInitEvent args)
    {
        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;

        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int)VisibilityFlags.PhantomVessel, eyeComponent);
    }

    private void OnVesselShutdown(EntityUid uid, VesselComponent component, ComponentShutdown args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;

        phantom.Vessels.Remove(uid);
        var ev = new RefreshPhantomLevelEvent();
        RaiseLocalEvent(component.Phantom, ref ev);
        PopulateVesselMenu(component.Phantom);

        if (phantom.Holder == uid)
            StopHaunt(component.Phantom, uid, phantom);

        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;
        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int)VisibilityFlags.PhantomVessel, eyeComponent);
    }

    private void OnVesselDeath(EntityUid uid, VesselComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            TryComp<PhantomComponent>(component.Phantom, out var comp);
            RemComp<VesselComponent>(uid);
            if (comp != null)
            {
                if (comp.TyranyStarted && comp.Vessels.Count <= 0)
                    ChangeEssenceAmount(component.Phantom, -1000, allowDeath: true);
            }
        }
    }

    private void OnVesselEctoplasmicDamage(EntityUid uid, VesselComponent component, EctoplasmHitscanHitEvent args)
    {
        _damageableSystem.TryChangeDamage(uid, args.DamageToTarget, true);
        StopHaunt(component.Phantom, uid);
    }

    private void OnVesselDeleted(EntityUid uid, VesselComponent component, EntityTerminatingEvent args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;

        phantom.Vessels.Remove(uid);
        var ev = new RefreshPhantomLevelEvent();
        RaiseLocalEvent(component.Phantom, ref ev);

        if (phantom.Holder == uid)
            StopHaunt(component.Phantom, uid, phantom);

        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;
        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int)VisibilityFlags.PhantomVessel, eyeComponent);
    }
    #endregion

    #region Puppet event handlers
    private void OnPupMapInit(EntityUid uid, PhantomPuppetComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ClawsActionEntity, component.ClawsAction);
        _action.AddAction(uid, ref component.HealActionEntity, component.HealAction);
    }

    private void OnPupShutdown(EntityUid uid, PhantomPuppetComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ClawsActionEntity);
        _action.RemoveAction(uid, component.HealActionEntity);
    }

    private void OnPupClaws(EntityUid uid, PhantomPuppetComponent component, SelfGhostClawsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!component.ClawsOn)
        {
            var claws = Spawn("ADTGhostClaws", Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(claws);

            _inventorySystem.TryUnequip(uid, "gloves", true, true, false, inventory);
            _inventorySystem.TryEquip(uid, claws, "gloves", true, true, false, inventory);
            component.Claws = claws;
            var message = Loc.GetString("phantom-claws-appear", ("name", Identity.Entity(uid, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-appear-self");
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), 10);
            _damageableSystem.TryChangeDamage(uid, damage_brute);
        }
        else
        {
            QueueDel(component.Claws);
            var message = Loc.GetString("phantom-claws-disappear", ("name", Identity.Entity(uid, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-disappear-self");
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        component.ClawsOn = !component.ClawsOn;
    }

    private void OnPupHeal(EntityUid uid, PhantomPuppetComponent component, SelfGhostHealActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(uid, damage_brute);
        _damageableSystem.TryChangeDamage(uid, damage_burn);
    }
    #endregion

    #region Utility
    /// <summary>
    /// Makes target a vessel
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    /// <returns>Is target became a vessel</returns>
    public bool TryMakeVessel(EntityUid uid, EntityUid target, PhantomComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out _))
            return false;

        if (component.Vessels.Count >= component.VesselsStrandCap)
            return false;

        _popup.PopupEntity(Loc.GetString("phantom-vessel-success-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);

        var vessel = EnsureComp<VesselComponent>(target);
        component.Vessels.Add(target);
        vessel.Phantom = uid;

        SelectStyle(uid, component, component.CurrentStyle, true);
        ChangeEssenceAmount(uid, 0, component);

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            _audio.PlayGlobal(component.GhostKissSound, mind.Session);

        PopulateVesselMenu(uid);

        return true;
    }

    public void PopulateVesselMenu(EntityUid uid, PhantomComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var ev = new PopulatePhantomVesselMenuEvent(GetNetEntity(uid), new());

        foreach (var vessel in comp.Vessels)
        {
            if (!TryComp<HumanoidAppearanceComponent>(vessel, out var humanoid))
                continue;

            var netEnt = GetNetEntity(vessel);

            // Here we build profiles for previews
            HumanoidCharacterAppearance hca = new();

            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.FacialHair, out var facialHair)
                && facialHair.TryGetValue(0, out var facialMarking))
            {
                hca = hca.WithFacialHairStyleName(facialMarking.MarkingId);
                hca = hca.WithFacialHairColor(facialMarking.MarkingColors.First());
            }
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Hair, out var hair)
                && hair.TryGetValue(0, out var hairMarking))
            {
                hca = hca.WithHairStyleName(hairMarking.MarkingId);
                hca = hca.WithHairColor(hairMarking.MarkingColors.First());
            }

            foreach (var item in Enum.GetValues<MarkingCategories>())
            {
                if (item is MarkingCategories.FacialHair or MarkingCategories.Hair)
                    continue;

                if (humanoid.MarkingSet.Markings.TryGetValue(item, out var mark))
                    hca = hca.WithMarkings(mark);
            }

            hca = hca.WithSkinColor(humanoid.SkinColor);
            hca = hca.WithEyeColor(humanoid.EyeColor);

            HumanoidCharacterProfile profile = new HumanoidCharacterProfile().WithCharacterAppearance(hca).WithSpecies(humanoid.Species);

            ev.Vessels.Add((netEnt, profile, Name(vessel)));
        }
        ev.Vessels.Sort();

        RaiseNetworkEvent(ev, uid);
    }
    #endregion
}
