using Content.Shared.Actions;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.Movement.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Content.Shared.IdentityManagement;
using Content.Shared.Chat;
using Content.Server.Chat;
using System.Linq;
using Content.Shared.Heretic;
using Content.Shared.Alert;
using Robust.Server.GameObjects;
using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using System.Numerics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Robust.Shared.Prototypes;
using Content.Shared.Stunnable;
using Content.Server.Power.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Bible.Components;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Server.EUI;
using Content.Server.Body.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.ADT.Hallucinations;
using Content.Server.AlertLevel;
using Content.Shared.ADT.Controlled;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.CombatMode;
using Content.Server.Cuffs;
using Robust.Server.Player;
using Content.Shared.Preferences;
using Content.Shared.Ghost;
using Content.Shared.Tag;
using Content.Server.Hands.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.ADT.Hallucinations;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.GhostInteractions;
using Content.Shared.Revenant.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.ADT.Silicon.Components;
using Content.Server.Singularity.Events;
using Content.Server.GameTicking.Rules;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem : SharedPhantomSystem
{
    #region Dependency
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedControlledSystem _controlled = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ApcSystem _apcSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PhantomRuleSystem _phantomGameRule = default!;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        // Startup
        SubscribeLocalEvent<PhantomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PhantomComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PhantomComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PhantomComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<PhantomComponent, StatusEffectEndedEvent>(OnStatusEnded);

        SubscribeLocalEvent<PhantomPuppetComponent, MapInitEvent>(OnPupMapInit);
        SubscribeLocalEvent<PhantomPuppetComponent, ComponentShutdown>(OnPupShutdown);
        SubscribeLocalEvent<PhantomPuppetComponent, SelfGhostClawsActionEvent>(OnPupClaws);
        SubscribeLocalEvent<PhantomPuppetComponent, SelfGhostHealActionEvent>(OnPupHeal);

        // Haunting
        SubscribeLocalEvent<PhantomComponent, MakeHolderActionEvent>(OnMakeHolder);
        SubscribeLocalEvent<PhantomComponent, StopHauntingActionEvent>(OnStopHaunting);
        SubscribeLocalEvent<PhantomComponent, UpdateCanMoveEvent>(OnTryMove);

        // Vessels manipulations
        SubscribeLocalEvent<PhantomComponent, HauntVesselActionEvent>(OnRequestVesselMenu);
        SubscribeNetworkEvent<SelectPhantomVesselEvent>(OnSelectVessel);

        SubscribeLocalEvent<PhantomComponent, MakeVesselActionEvent>(OnMakeVessel);
        SubscribeLocalEvent<PhantomComponent, MakeVesselDoAfterEvent>(MakeVesselDoAfter);

        // Abilities
        SubscribeLocalEvent<PhantomComponent, ParalysisActionEvent>(OnParalysis);
        SubscribeLocalEvent<PhantomComponent, MaterializeActionEvent>(OnCorporeal);
        SubscribeLocalEvent<PhantomComponent, BreakdownActionEvent>(OnBreakdown);
        SubscribeLocalEvent<PhantomComponent, StarvationActionEvent>(OnStarvation);
        SubscribeLocalEvent<PhantomComponent, ShieldBreakActionEvent>(OnShieldBreak);
        SubscribeLocalEvent<PhantomComponent, GhostClawsActionEvent>(OnGhostClaws);
        SubscribeLocalEvent<PhantomComponent, GhostInjuryActionEvent>(OnGhostInjury);
        SubscribeLocalEvent<PhantomComponent, GhostHealActionEvent>(OnGhostHeal);
        SubscribeLocalEvent<PhantomComponent, PuppeterActionEvent>(OnPuppeter);
        SubscribeLocalEvent<PhantomComponent, PuppeterDoAfterEvent>(PuppeterDoAfter);
        SubscribeLocalEvent<PhantomComponent, RepairActionEvent>(OnRepair);
        SubscribeLocalEvent<PhantomComponent, BloodBlindingActionEvent>(OnBlinding);
        SubscribeLocalEvent<PhantomComponent, PhantomPortalActionEvent>(OnPortal);
        SubscribeLocalEvent<PhantomComponent, PsychoEpidemicActionEvent>(OnPsychoEpidemic);
        SubscribeLocalEvent<PhantomComponent, PhantomHelpingHelpActionEvent>(OnHelpingHand);
        SubscribeLocalEvent<PhantomComponent, PhantomControlActionEvent>(OnControl);

        // Finales
        SubscribeLocalEvent<PhantomComponent, NightmareFinaleActionEvent>(OnNightmare);
        SubscribeLocalEvent<PhantomComponent, TyranyFinaleActionEvent>(OnTyrany);
        SubscribeLocalEvent<PhantomComponent, FreedomFinaleActionEvent>(OnRequestFreedomMenu);
        SubscribeNetworkEvent<SelectPhantomFreedomEvent>(OnSelectFreedom);

        // Other
        SubscribeLocalEvent<PhantomComponent, AlternativeSpeechEvent>(OnTrySpeak);
        SubscribeLocalEvent<PhantomComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<PhantomComponent, RefreshPhantomLevelEvent>(OnLevelChanged);
        SubscribeLocalEvent<PhantomComponent, OpenPhantomStylesMenuActionEvent>(OnRequestStyleMenu);
        SubscribeNetworkEvent<SelectPhantomStyleEvent>(OnSelectStyle);
        SubscribeLocalEvent<PhantomComponent, EventHorizonAttemptConsumeEntityEvent>(OnSinguloConsumeAttempt);
        SubscribeLocalEvent<PhantomComponent, StopHauntAlertEvent>(OnStopHauntAlertClick);

        // IDK why the fuck this is not working in another file
        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";
    public const string GlovesId = "gloves";
    public const string OuterClothingId = "outerClothing";

    private void OnStartup(EntityUid uid, PhantomComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, PhantomVisuals.Haunting, false);
        _appearance.SetData(uid, PhantomVisuals.Stunned, false);
        _appearance.SetData(uid, PhantomVisuals.Corporeal, false);
        ChangeEssenceAmount(uid, 0, component);
    }

    private void OnMapInit(EntityUid uid, PhantomComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.PhantomHauntActionEntity, component.PhantomHauntAction);
        _action.AddAction(uid, ref component.PhantomMakeVesselActionEntity, component.PhantomMakeVesselAction);
        _action.AddAction(uid, ref component.PhantomStyleActionEntity, component.PhantomStyleAction);
        _action.AddAction(uid, ref component.PhantomHauntVesselActionEntity, component.PhantomHauntVesselAction);
        SelectStyle(uid, component, component.CurrentStyle, true);

        if (TryComp<EyeComponent>(uid, out var eyeComponent))
            _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int) VisibilityFlags.PhantomVessel, eyeComponent);

        component.HelpingHand = _container.EnsureContainer<Container>(uid, "HelpingHand");
    }

    private void OnShutdown(EntityUid uid, PhantomComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.PhantomHauntActionEntity);
        _action.RemoveAction(uid, component.PhantomMakeVesselActionEntity);
        _action.RemoveAction(uid, component.PhantomStyleActionEntity);
        //_action.RemoveAction(uid, component.PhantomSelectVesselActionEntity);
        _action.RemoveAction(uid, component.PhantomHauntVesselActionEntity);
        foreach (var action in component.CurrentActions)
        {
            _action.RemoveAction(uid, action);
            if (action != null)
                QueueDel(action.Value);
        }
        if (TryComp<EyeComponent>(uid, out var eyeComponent))
            _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | ~(int) VisibilityFlags.PhantomVessel, eyeComponent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhantomComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator <= 1)
                continue;
            comp.Accumulator -= 1;

            if (comp.HelpingHandTimer > 0)
                comp.HelpingHandTimer -= 1;

            if (comp.SpeechTimer > 0)
                comp.SpeechTimer -= 1;

            if (comp.HelpingHandTimer <= 0 && comp.HelpingHand.ContainedEntities.Count > 0 && !Deleted(comp.TransferringEntity))
                _container.TryRemoveFromContainer(comp.TransferringEntity, true);

            if (comp.Essence < comp.EssenceRegenCap && !CheckAltars(uid, comp) && comp.HasHaunted)
                ChangeEssenceAmount(uid, comp.EssencePerSecond, comp, regenCap: true);

            if (CheckAltars(uid, comp))
                ChangeEssenceAmount(uid, comp.ChurchDamage, comp, true);
        }
    }

    private void OnStatusAdded(EntityUid uid, PhantomComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, PhantomVisuals.Stunned, true);
        Dirty(uid, component);
    }

    private void OnStatusEnded(EntityUid uid, PhantomComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
        {
            _appearance.SetData(uid, PhantomVisuals.Stunned, false);
            _appearance.SetData(uid, PhantomVisuals.Corporeal, component.IsCorporeal);
        }
        Dirty(uid, component);
    }

    #region Radial Menu
    /// <summary>
    /// Requests radial menu for the styles
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="args">Event</param>
    private void OnRequestStyleMenu(EntityUid uid, PhantomComponent component, OpenPhantomStylesMenuActionEvent args)
    {
        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
        {
            var ev = new RequestPhantomStyleMenuEvent(GetNetEntity(uid));

            foreach (var prototype in _proto.EnumeratePrototypes<PhantomStylePrototype>())
            {
                if (prototype.Icon == null)
                    continue;
                ev.Prototypes.Add(prototype.ID);
            }
            ev.Prototypes.Sort();
            RaiseNetworkEvent(ev, mind.Session);
        }
    }

    /// <summary>
    /// Raised when style selected
    /// </summary>
    /// <param name="args">Event</param>
    private void OnSelectStyle(SelectPhantomStyleEvent args)
    {
        var uid = GetEntity(args.Target);
        if (!TryComp<PhantomComponent>(uid, out var comp))
            return;
        if (args.Handled)
            return;
        if (args.PrototypeId == comp.CurrentStyle)
        {
            var selfMessage = Loc.GetString("phantom-style-already");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        SelectStyle(uid, comp, args.PrototypeId);
        args.Handled = true;
    }

    /// <summary>
    /// Requests radial menu for freedom finale
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="args">Event</param>
    private void OnRequestFreedomMenu(EntityUid uid, PhantomComponent component, FreedomFinaleActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.FinalAbilityUsed)
        {
            var selfMessage = Loc.GetString("phantom-final-already");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
        {
            var ev = new RequestPhantomFreedomMenuEvent(GetNetEntity(uid));

            foreach (var prototype in _proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!prototype.TryGetComponent<InstantActionComponent>(out var actionComp))
                    continue;
                if (!prototype.TryGetComponent<TagComponent>(out var tag))
                    continue;
                if (actionComp.Icon == null)
                    continue;
                foreach (var whitelisted in tag.Tags.ToList())
                {
                    if (whitelisted == "PhantomFreedom")
                        ev.Prototypes.Add(prototype.ID);
                }
            }
            ev.Prototypes.Sort();
            RaiseNetworkEvent(ev, mind.Session);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Raised when freedom type selected
    /// </summary>
    /// <param name="args">Event</param>
    private void OnSelectFreedom(SelectPhantomFreedomEvent args)
    {
        var uid = GetEntity(args.Target);
        if (!TryComp<PhantomComponent>(uid, out var component))
            return;

        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;
        if (!_mindSystem.TryGetMind(uid, out _, out var mind) || mind.Session == null)
            return;

        if (args.PrototypeId == "ActionPhantomOblivion")
        {
            var eui = new PhantomFinaleEui(uid, this, component, PhantomFinaleType.Oblivion);
            _euiManager.OpenEui(eui, mind.Session);
        }
        if (args.PrototypeId == "ActionPhantomDeathmatch")
        {
            var eui = new PhantomFinaleEui(uid, this, component, PhantomFinaleType.Deathmatch);
            _euiManager.OpenEui(eui, mind.Session);
        }
        if (args.PrototypeId == "ActionPhantomHelpFin")
        {
            var eui = new PhantomFinaleEui(uid, this, component, PhantomFinaleType.Help);
            _euiManager.OpenEui(eui, mind.Session);
        }
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
        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
        {
            var ev = new RequestPhantomVesselMenuEvent(GetNetEntity(uid), new());

            foreach (var vessel in component.Vessels)
            {
                if (!TryComp<HumanoidAppearanceComponent>(vessel, out var humanoid))
                    continue;
                if (!TryComp<MetaDataComponent>(vessel, out var meta))
                    continue;
                var netEnt = GetNetEntity(vessel);

                HumanoidCharacterAppearance hca = new();

                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.FacialHair, out var facialHair))
                    if (facialHair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithFacialHairStyleName(marking.MarkingId);
                        hca = hca.WithFacialHairColor(marking.MarkingColors.First());
                    }
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Hair, out var hair))
                    if (hair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithHairStyleName(marking.MarkingId);
                        hca = hca.WithHairColor(marking.MarkingColors.First());
                    }
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Head, out var head))
                    hca = hca.WithMarkings(head);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.HeadSide, out var headSide))
                    hca = hca.WithMarkings(headSide);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.HeadTop, out var headTop))
                    hca = hca.WithMarkings(headTop);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Snout, out var snout))
                    hca = hca.WithMarkings(snout);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Chest, out var chest))
                    hca = hca.WithMarkings(chest);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Arms, out var arms))
                    hca = hca.WithMarkings(arms);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Legs, out var legs))
                    hca = hca.WithMarkings(legs);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var tail))
                    hca = hca.WithMarkings(tail);
                if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Overlay, out var overlay))
                    hca = hca.WithMarkings(overlay);

                hca = hca.WithSkinColor(humanoid.SkinColor);
                hca = hca.WithEyeColor(humanoid.EyeColor);
                //hca = hca.WithMarkings(humanoid.MarkingSet.Markings.);

                HumanoidCharacterProfile profile = new HumanoidCharacterProfile().WithCharacterAppearance(hca).WithSpecies(humanoid.Species);

                ev.Vessels.Add((netEnt, profile, meta.EntityName));
            }
            ev.Vessels.Sort();
            RaiseNetworkEvent(ev, mind.Session);
        }

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
            Haunt(uid, target, comp);
        else
        {
            StopHaunt(uid, comp.Holder, comp);
            Haunt(uid, target, comp);
        }
    }
    #endregion

    #region Abilities
    private void OnMakeHolder(EntityUid uid, PhantomComponent component, MakeHolderActionEvent args)
    {
        if (args.Handled)
            return;
        if (!component.CanHaunt)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, target))
            return;

        if (!component.HasHaunted)
            Haunt(uid, target, component);
        else
        {
            StopHaunt(uid, component.Holder, component);
            Haunt(uid, target, component);
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

        if (!_mindSystem.TryGetMind(target, out var mindId, out var mind) || mind.Session == null)
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

    private void OnParalysis(EntityUid uid, PhantomComponent component, ParalysisActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (HasComp<MindShieldComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-mindshield", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, target))
            return;

        if (IsHolder(target, component))
        {
            if (!component.ParalysisOn)
            {
                UpdateEctoplasmSpawn(uid);
                var timeHaunted = TimeSpan.FromHours(1);
                _status.TryAddStatusEffect<KnockedDownComponent>(target, "KnockedDown", timeHaunted, false);
                _status.TryAddStatusEffect<StunnedComponent>(target, "Stun", timeHaunted, false);
                if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                    _audio.PlayGlobal(component.ParalysisSound, mind.Session);
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
                return;
            }
            else
            {
                UpdateEctoplasmSpawn(uid);
                var time = TimeSpan.FromSeconds(10);
                if (_status.TryAddStatusEffect<KnockedDownComponent>(target, "KnockedDown", time, false) && _status.TryAddStatusEffect<StunnedComponent>(target, "Stun", time, false))
                {
                    if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                        _audio.PlayGlobal(component.ParalysisSound, mind.Session);
                    args.Handled = true;
                }
            }
        }
    }

    private void OnCorporeal(EntityUid uid, PhantomComponent component, MaterializeActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.HasHaunted)
            return;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physicsSystem.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), fixtures);
            _physicsSystem.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
        }
        var visibility = EnsureComp<VisibilityComponent>(uid);

        _visibility.SetLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
        _visibility.RefreshVisibility(uid);

        component.IsCorporeal = true;
    }

    private void OnBreakdown(EntityUid uid, PhantomComponent component, BreakdownActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, target))
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
                    _status.TryAddStatusEffect<KnockedDownComponent>(target, "KnockedDown", time, false, status);
                    _status.TryAddStatusEffect<StunnedComponent>(target, "Stun", time, false, status);
                    _status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", timeHaunted, false, status);
                    if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                        _audio.PlayGlobal(component.BreakdownSound, mind.Session);
                    if (_mindSystem.TryGetMind(target, out _, out var targetMind) && targetMind.Session != null)
                        _audio.PlayGlobal(component.BreakdownSound, targetMind.Session);

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
                _status.TryAddStatusEffect<KnockedDownComponent>(target, "KnockedDown", time, false);
                _status.TryAddStatusEffect<StunnedComponent>(target, "Stun", time, false);
                _status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", timeStatic, false);
                success = true;
            }

            if (TryComp<ContainerManagerComponent>(target, out var containerManagerComponent))
            {
                foreach (var container in containerManagerComponent.Containers.Values)
                {
                    foreach (var entity in container.ContainedEntities)
                    {
                        if (TryComp<PoweredLightComponent>(entity, out var entLight))
                        {
                            _poweredLight.TryDestroyBulb(entity, entLight);
                            success = true;
                        }
                        if (TryComp<ApcComponent>(entity, out var entApc))
                        {
                            _apcSystem.ApcToggleBreaker(entity, entApc);
                            success = true;
                        }
                        if (HasComp<StatusEffectsComponent>(entity) && HasComp<BatteryComponent>(entity) && _status.TryAddStatusEffect<SeeingStaticComponent>(entity, "SeeingStatic", timeStatic, false))
                        {
                            _status.TryAddStatusEffect<KnockedDownComponent>(entity, "KnockedDown", time, false);
                            _status.TryAddStatusEffect<StunnedComponent>(entity, "Stun", time, false);
                            _status.TryAddStatusEffect<SlowedDownComponent>(entity, "SlowedDown", timeStatic, false);
                            success = true;
                        }
                    }
                }
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
                _audio.PlayPvs(component.BreakdownSound, target);
            }
            else
            {
                var selfMessage = Loc.GetString("phantom-breakdown-fail");
                _popup.PopupEntity(selfMessage, uid, uid);
            }
        }
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
                if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                    _audio.PlayGlobal(component.InjurySound, mind.Session);
            }
        }
    }

    private void OnGhostInjury(EntityUid uid, PhantomComponent component, GhostInjuryActionEvent args)
    {
        if (args.Handled)
            return;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            _audio.PlayGlobal(component.InjurySound, mind.Session);

        foreach (var mob in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 12f))
        {
            if (HasComp<VesselComponent>(mob))
                continue;
            if (!HasComp<HumanoidAppearanceComponent>(mob))
                continue;
            if (!TryUseAbility(uid, mob))
                continue;

            if (_mindSystem.TryGetMind(mob, out _, out var targetMind) && targetMind.Session != null)
                _audio.PlayGlobal(component.InjurySound, targetMind.Session);

            var stunTime = TimeSpan.FromSeconds(2);
            var list = _proto.EnumeratePrototypes<DamageGroupPrototype>().ToList();

            var damage = new DamageSpecifier(_random.Pick(list), component.InjuryDamage);
            _damageableSystem.TryChangeDamage(mob, damage);
            _sharedStun.TryParalyze(mob, stunTime, true);
        }
    }

    private void OnGhostClaws(EntityUid uid, PhantomComponent component, GhostClawsActionEvent args)
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

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (!TryComp(target, out InventoryComponent? inventory))
            return;

        if (!component.ClawsOn)
        {
            var claws = Spawn("ADTGhostClaws", Transform(target).Coordinates);
            EnsureComp<UnremoveableComponent>(claws);

            _inventorySystem.TryUnequip(target, GlovesId, true, true, false, inventory);
            _inventorySystem.TryEquip(target, claws, GlovesId, true, true, false, inventory);
            component.Claws = claws;
            var message = Loc.GetString("phantom-claws-appear", ("name", Identity.Entity(target, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-appear-self");
            _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, target, target, PopupType.MediumCaution);
        }
        else
        {
            QueueDel(component.Claws);
            var message = Loc.GetString("phantom-claws-disappear", ("name", Identity.Entity(target, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-disappear-self");
            _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, target, target, PopupType.MediumCaution);

            component.Claws = new();
        }
        component.ClawsOn = !component.ClawsOn;
    }

    private void OnShieldBreak(EntityUid uid, PhantomComponent component, ShieldBreakActionEvent args)
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

        if (!TryComp<MindShieldComponent>(target, out var mindShield))
        {
            var selfMessage = Loc.GetString("phantom-no-mindshield");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        float chance = 0f;

        if (component.Vessels.Count > 5)
            chance = 0.3f;
        if (component.Vessels.Count > 7)
            chance = 0.5f;
        if (component.Vessels.Count > 9)
            chance = 0.8f;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (_random.Prob(chance))
        {
            var stunTime = TimeSpan.FromSeconds(1);
            RemComp<MindShieldComponent>(target);
            _sharedStun.TryParalyze(target, stunTime, true);
            _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
            _popup.PopupEntity(Loc.GetString("phantom-mindshield-success-target"), target, target, PopupType.MediumCaution);
        }
        else
        {
            var stunTime = TimeSpan.FromSeconds(1);
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-self", ("name", Identity.Entity(target, EntityManager))), uid, uid);
            _popup.PopupEntity(Loc.GetString("phantom-mindshield-fail-target"), uid, uid, PopupType.SmallCaution);
        }
    }

    private void OnGhostHeal(EntityUid uid, PhantomComponent component, GhostHealActionEvent args)
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

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            _audio.PlayGlobal(component.RecoverySound, mind.Session);
        if (_mindSystem.TryGetMind(target, out _, out var targetMind) && targetMind.Session != null)
            _audio.PlayGlobal(component.RecoverySound, targetMind.Session);


        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(target, damage_brute);
        _damageableSystem.TryChangeDamage(target, damage_burn);
    }

    // Oath but im too lazy to rename it
    private void OnPuppeter(EntityUid uid, PhantomComponent component, PuppeterActionEvent args)
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

    private void OnRepair(EntityUid uid, PhantomComponent component, RepairActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, target))
            return;

        bool success = false;
        if (TryComp<BatteryComponent>(target, out var batteryComp))
        {
            _batterySystem.SetCharge(target, batteryComp.MaxCharge);
            success = true;
        }

        if (HasComp<BatteryComponent>(target) || HasComp<PowerCellSlotComponent>(target))
        {
            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
            _damageableSystem.TryChangeDamage(target, damage_brute);
            _damageableSystem.TryChangeDamage(target, damage_burn);
            success = true;
        }

        if (TryComp<ContainerManagerComponent>(target, out var containerManagerComponent))
        {
            foreach (var container in containerManagerComponent.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<BatteryComponent>(entity, out var entBatteryComp))
                    {
                        _batterySystem.SetCharge(entity, entBatteryComp.MaxCharge);
                        success = true;
                    }

                    if (HasComp<BatteryComponent>(target) || HasComp<PowerCellSlotComponent>(target))
                    {
                        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
                        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
                        _damageableSystem.TryChangeDamage(target, damage_brute);
                        _damageableSystem.TryChangeDamage(target, damage_burn);
                        success = true;
                    }
                }
            }
        }

        if (success)
        {
            if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                _audio.PlayGlobal(component.RepairSound, mind.Session);
            var selfMessage = Loc.GetString("phantom-repair-self", ("name", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
        }
    }

    private void OnBlinding(EntityUid uid, PhantomComponent component, BloodBlindingActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, target))
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            var selfMessage = Loc.GetString("phantom-blinding-noblood");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryComp<StatusEffectsComponent>(target, out var status))
        {
            var selfMessage = Loc.GetString("phantom-blinding-noblood");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryComp<BlindableComponent>(target, out var blindable))
        {
            var selfMessage = Loc.GetString("phantom-blinding-noblood");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<SiliconComponent>(target))
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

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            _audio.PlayGlobal(component.BlindingSound, mind.Session);
        if (_mindSystem.TryGetMind(target, out _, out var targetMind) && targetMind.Session != null)
            _audio.PlayGlobal(component.BlindingSound, targetMind.Session);

        _bloodstreamSystem.TryModifyBleedAmount(target, component.BlindingBleed, bloodstream);
        _status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, "TemporaryBlindness", component.BlindingTime, true);
        _blindable.AdjustEyeDamage((target, blindable), 3);
        var targetMessage = Loc.GetString("phantom-blinding-target");
        var message = Loc.GetString("phantom-blinding-others", ("name", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(targetMessage, target, target, PopupType.LargeCaution);
        _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
    }

    private void OnPortal(EntityUid uid, PhantomComponent component, PhantomPortalActionEvent args)
    {
        if (args.Handled)
            return;

        var coordinates = Transform(uid).Coordinates;

        if (Deleted(component.Portal1) && Deleted(component.Portal2))
        {
            var portal = Spawn(component.PortalPrototype, coordinates);
            EnsureComp<PhantomPortalComponent>(portal);
            _visibility.SetLayer((portal, EnsureComp<VisibilityComponent>(portal)), (int) VisibilityFlags.PhantomVessel);
            _visibility.RefreshVisibility(portal);
            component.Portal1 = portal;
            _audio.PlayPvs(component.PortalSound, portal);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }

        if (!Deleted(component.Portal1) && Deleted(component.Portal2))
        {
            if (Transform(component.Portal1).Coordinates.TryDistance(EntityManager, coordinates, out var distance) &&
                distance > 10f)
            {
                var message = Loc.GetString("phantom-portal-too-far");
                _popup.PopupEntity(message, uid, uid);
                return;
            }

            var portal = Spawn(component.PortalPrototype, coordinates);
            _visibility.SetLayer((portal, EnsureComp<VisibilityComponent>(portal)), (int) VisibilityFlags.PhantomVessel);
            _visibility.RefreshVisibility(portal);
            var firstPortalComp = EnsureComp<PhantomPortalComponent>(component.Portal1);
            var secondPortalComp = EnsureComp<PhantomPortalComponent>(portal);
            component.Portal2 = portal;
            firstPortalComp.LinkedPortal = portal;
            secondPortalComp.LinkedPortal = component.Portal1;
            _audio.PlayPvs(component.PortalSound, portal);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }

        if (!Deleted(component.Portal1) && !Deleted(component.Portal2))
        {
            QueueDel(component.Portal1);
            QueueDel(component.Portal2);
            component.Portal1 = new();
            component.Portal2 = new();
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }
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

        var (target, _) = _random.Pick(_lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, 150f));

        if (!_hallucinations.StartEpidemicHallucinations(target, component.HallucinationsPrototype))
        {
            var failMessage = Loc.GetString("phantom-epidemic-fail");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            _audio.PlayGlobal(component.PsychoSound, mind.Session);

        var selfMessage = Loc.GetString("phantom-epidemic-success", ("name", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessage, uid, uid);

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;
    }

    private void OnHelpingHand(EntityUid uid, PhantomComponent component, PhantomHelpingHelpActionEvent args)
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

        if (!_mindSystem.TryGetMind(target, out _, out var mind) || mind.Session == null)
        {
            var failMessage = Loc.GetString("phantom-no-mind");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        args.Handled = true;

        UpdateEctoplasmSpawn(uid);
        _euiManager.OpenEui(new AcceptHelpingHandEui(target, this, component), mind.Session);
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

        if (!TryUseAbility(uid, target))
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

        if (_mindSystem.TryGetMind(uid, out _, out var mind))// &&
        //_mindSystem.TryGetMind(target, out _, out var targetMind))
        {
            args.Handled = true;
            if (mind.Session != null)
                _audio.PlayGlobal(component.PuppeterSound, mind.Session);
            if (_mindSystem.TryGetMind(target, out _, out var targetMind) && targetMind.Session != null)
                _audio.PlayGlobal(component.PuppeterSound, targetMind.Session);
            UpdateEctoplasmSpawn(uid);
            _controlled.TryStartControlling(uid, target, 30f, 10, "Phantom");
        }

    }

    private void OnNightmare(EntityUid uid, PhantomComponent component, NightmareFinaleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (component.FinalAbilityUsed)
        {
            var selfMessage = Loc.GetString("phantom-final-already");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        var target = component.Holder;

        if (HasComp<MindShieldComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-fail-mindshield", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, target))
            return;

        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;
        if (!_mindSystem.TryGetMind(uid, out _, out var selfMind) || selfMind.Session == null)
            return;
        if (!_mindSystem.TryGetMind(target, out _, out var mind) || mind.Session == null)
        {
            var failMessage = Loc.GetString("phantom-no-mind");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        args.Handled = true;

        var eui = new PhantomFinaleEui(uid, this, component, PhantomFinaleType.Nightmare);
        _euiManager.OpenEui(eui, selfMind.Session);
    }

    private void OnTyrany(EntityUid uid, PhantomComponent component, TyranyFinaleActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder-need");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (component.FinalAbilityUsed)
        {
            var selfMessage = Loc.GetString("phantom-final-already");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;
        if (!_mindSystem.TryGetMind(uid, out _, out var mind) || mind.Session == null)
            return;

        args.Handled = true;

        var eui = new PhantomFinaleEui(uid, this, component, PhantomFinaleType.Tyrany);
        _euiManager.OpenEui(eui, mind.Session);
    }
    #endregion

    #region Other
    public void OnTrySpeak(EntityUid uid, PhantomComponent component, AlternativeSpeechEvent args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 8f))
        {
            if (TryComp<GhostRadioComponent>(ent, out var radio) && radio.Enabled)
                _chatSystem.TrySendInGameICMessage(ent, args.Message, InGameICChatType.Whisper, false, ignoreActionBlocker: true);
        }

        bool playSound = false;
        if (!component.IsCorporeal)
        {
            args.Cancel();

            foreach (var ghost in _playerManager.Sessions)
            {
                var ent = ghost.AttachedEntity;
                if (!HasComp<GhostComponent>(ent))
                    continue;
                var wrappedMessage = Loc.GetString("chat-manager-entity-say-wrap-message",
                    ("entityName", Identity.Entity(uid, EntityManager)),
                    ("verb", "шепчет"),
                    ("fontType", "Default"),
                    ("fontSize", 12),
                    ("message", args.Message));

                _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, wrappedMessage, uid, false, ghost.Channel);
            }
            if (args.Radio)
            {
                var popupMessage = Loc.GetString("phantom-say-target");
                var selfMessage = Loc.GetString("phantom-say-all-vessels-self");
                var message = popupMessage == "" ? "" : popupMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");
                var selfChatMessage = selfMessage == "" ? "" : selfMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");

                if (!_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) || selfMind.Session == null)
                    return;
                _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, selfChatMessage, EntityUid.Invalid, false, selfMind.Session.Channel);
                _popup.PopupEntity(selfMessage, uid, uid);

                foreach (var vessel in component.Vessels)
                {
                    if (!HasComp<VesselComponent>(vessel) && !HasComp<PhantomHolderComponent>(vessel))
                        continue;
                    if (!_mindSystem.TryGetMind(vessel, out var mindId, out var mind) || mind.Session == null)
                        continue;
                    _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, message, EntityUid.Invalid, false, mind.Session.Channel);
                    _popup.PopupEntity(popupMessage, vessel, vessel, PopupType.MediumCaution);

                    if (component.SpeechTimer <= 0)
                    {
                        _audio.PlayGlobal(component.SpeechSound, mind.Session);
                        component.SpeechTimer = 5;
                        playSound = true;
                    }
                }
                if (playSound)
                    _audio.PlayGlobal(component.SpeechSound, selfMind.Session);
                return;
            }
            if (args.Type == InGameICChatType.Speak)
            {
                var popupMessage = Loc.GetString("phantom-say-target");
                var selfMessage = Loc.GetString("phantom-say-near-vessels-self");
                var message = popupMessage == "" ? "" : popupMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");
                var selfChatMessage = selfMessage == "" ? "" : selfMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");

                if (!_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) || selfMind.Session == null)
                    return;
                _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, selfChatMessage, EntityUid.Invalid, false, selfMind.Session.Channel);
                _popup.PopupEntity(selfMessage, uid, uid);

                foreach (var vessel in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 7f))
                {
                    if (!HasComp<VesselComponent>(vessel) && !HasComp<PhantomHolderComponent>(vessel))
                        continue;
                    if (!_mindSystem.TryGetMind(vessel, out var mindId, out var mind) || mind.Session == null)
                        continue;
                    _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, message, EntityUid.Invalid, false, mind.Session.Channel);
                    _popup.PopupEntity(popupMessage, vessel, vessel, PopupType.MediumCaution);

                    if (component.SpeechTimer <= 0)
                    {
                        _audio.PlayGlobal(component.SpeechSound, mind.Session);
                        component.SpeechTimer = 5;
                        playSound = true;
                    }

                }
                if (playSound)
                    _audio.PlayGlobal(component.SpeechSound, selfMind.Session);
                return;
            }
            if (args.Type == InGameICChatType.Whisper)
            {
                if (component.HasHaunted)
                {
                    var target = component.Holder;
                    var popupMessage = Loc.GetString("phantom-say-target");
                    var selfMessage = Loc.GetString("phantom-say-self");

                    if (!_mindSystem.TryGetMind(target, out var mindId, out var mind) || mind.Session == null)
                        return;
                    if (!_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) || selfMind.Session == null)
                        return;

                    _popup.PopupEntity(popupMessage, target, target, PopupType.MediumCaution);
                    _popup.PopupEntity(selfMessage, uid, uid);

                    var message = popupMessage == "" ? "" : popupMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");
                    _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, message, EntityUid.Invalid, false, mind.Session.Channel);

                    var selfChatMessage = selfMessage == "" ? "" : selfMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");
                    _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, selfChatMessage, EntityUid.Invalid, false, selfMind.Session.Channel);

                    if (component.SpeechTimer <= 0)
                    {
                        _audio.PlayGlobal(component.SpeechSound, selfMind.Session);
                        _audio.PlayGlobal(component.SpeechSound, mind.Session);
                        component.SpeechTimer = 5;
                    }
                }
                else
                {
                    var selfMessage = Loc.GetString("phantom-say-fail");
                    _popup.PopupEntity(selfMessage, uid, uid);

                }
                return;
            }
        }
        else
        {
            if (args.Radio)
            {
                var popupMessage = Loc.GetString("phantom-say-target");
                var selfMessage = Loc.GetString("phantom-say-all-vessels-self");
                var message = popupMessage == "" ? "" : popupMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");
                var selfChatMessage = selfMessage == "" ? "" : selfMessage + (args.Message == "" ? "" : $" \"{args.Message}\"");

                if (!_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) || selfMind.Session == null)
                    return;
                _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, selfChatMessage, EntityUid.Invalid, false, selfMind.Session.Channel);
                _popup.PopupEntity(selfMessage, uid, uid);

                foreach (var vessel in component.Vessels)
                {
                    if (!_mindSystem.TryGetMind(vessel, out var mindId, out var mind) || mind.Session == null)
                        continue;
                    _chatManager.ChatMessageToOne(ChatChannel.Local, args.Message, message, EntityUid.Invalid, false, mind.Session.Channel);
                    _popup.PopupEntity(popupMessage, vessel, vessel, PopupType.MediumCaution);

                    if (component.SpeechTimer <= 0 && !HasComp<GhostComponent>(vessel))
                    {
                        _audio.PlayGlobal(component.SpeechSound, mind.Session);
                        component.SpeechTimer = 5;
                        playSound = true;
                    }
                }
                if (playSound)
                    _audio.PlayGlobal(component.SpeechSound, selfMind.Session);
                return;
            }
        }
    }

    /// <summary>
    /// Changes essense amount instead of getting damage
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="args">Event</param>
    private void OnDamage(EntityUid uid, PhantomComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;
        if (component.TyranyStarted)
            return;

        var essenceDamage = args.DamageDelta.GetTotal().Float() * -1;

        ChangeEssenceAmount(uid, essenceDamage, component);
    }

    private void OnTryMove(EntityUid uid, PhantomComponent component, UpdateCanMoveEvent args)
    {
        if (!component.HasHaunted)
            return;

        args.Cancel();
    }

    private void OnLevelChanged(EntityUid uid, PhantomComponent component, ref RefreshPhantomLevelEvent args)
    {
        SelectStyle(uid, component, component.CurrentStyle, true);
        _alerts.ShowAlert(uid, _proto.Index(component.VesselCountAlert), (short) Math.Clamp(component.Vessels.Count, 0, 10));
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

        var doMakeVessel = true;
        if (TryComp<HumanoidAppearanceComponent>(uid, out _))
        {
            foreach (var storedVessels in component.Vessels)
            {
                if (storedVessels == target)
                    doMakeVessel = false;
            }
        }

        if (doMakeVessel)
        {
            if (!MakeVessel(uid, target, component))
            {
                return;
            }
        }
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

        if (!_mindSystem.TryGetMind(target, out _, out var mind) || mind.Session == null)
            return;

        _euiManager.OpenEui(new AcceptPhantomPowersEui(target, this, component), mind.Session);
    }

    private void OnSinguloConsumeAttempt(EntityUid uid, PhantomComponent component, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStopHauntAlertClick(EntityUid uid, PhantomComponent component, ref StopHauntAlertEvent args)
    {
        StopHaunt(uid, component.Holder, component);
    }
    #endregion

    #region Raised Not By Events
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

    /// <summary>
    /// Reviving phantom if there is any vessels.
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <returns>Is phantom revived</returns>
    public bool TryRevive(EntityUid uid, PhantomComponent component)
    {
        if (component.Vessels.Count < 1)
            return false;
        var allowedVessels = new List<EntityUid>();
        foreach (var vessel in component.Vessels)
        {
            if (!CheckAltars(vessel, component) && TryUseAbility(uid, vessel))
                allowedVessels.Add(vessel);
        }
        if (allowedVessels.Count < 1)
        {
            var ev = new PhantomDiedEvent();
            RaiseLocalEvent(uid, ref ev);
            return false;
        }

        var randomVessel = _random.Pick(allowedVessels);
        component.Essence = 50;
        ChangeEssenceAmount(uid, 0, component, false);

        if (!component.HasHaunted)
        {
            Haunt(uid, randomVessel, component);
        }
        else
        {
            StopHaunt(uid, component.Holder, component);
            Haunt(uid, randomVessel, component);
        }
        return true;
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
    /// Makes target a vessel
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    /// <returns>Is target became a vessel</returns>
    public bool MakeVessel(EntityUid uid, EntityUid target, PhantomComponent component)
    {
        if (!TryComp<MetaDataComponent>(target, out _))
            return false;
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

    /// <summary>
    /// Haunt target
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    public void Haunt(EntityUid uid, EntityUid target, PhantomComponent component)
    {
        if (!component.CanHaunt)
            return;

        if (TryHaunt(uid, target))
        {
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

            // var eye = EnsureComp<EyeComponent>(uid);
            // _eye.SetDrawFov(uid, true, eye);
            _appearance.SetData(uid, PhantomVisuals.Haunting, true);

            var xform = Transform(uid);
            ContainerSystem.AttachParentToContainerOrGrid((uid, xform));

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

                    _physicsSystem.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) CollisionGroup.GhostImpassable, fixtures);
                    _physicsSystem.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);
                }
                var visibility = EnsureComp<VisibilityComponent>(uid);

                _visibility.SetLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibility.RefreshVisibility(uid);

                if (component.Vessels.Count >= 2)
                {
                    //_action.AddAction(uid, ref component.PhantomCorporealActionEntity, component.PhantomCorporealAction);
                    //_action.SetCooldown(component.PhantomCorporealActionEntity, component.Cooldown);
                }

                component.IsCorporeal = false;
            }

            _physicsSystem.SetLinearVelocity(uid, Vector2.Zero);

            var selfMessage = Loc.GetString("phantom-haunt-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("phantom-haunt-target");
            _popup.PopupEntity(targetMessage, target, target);

            _alerts.ShowAlert(uid, component.HauntedAlert);
            //_action.AddAction(uid, ref component.PhantomStopHauntActionEntity, component.PhantomStopHauntAction);

            if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                _audio.PlayGlobal(component.HauntSound, mind.Session);

            Dirty(target, holderComp);

        }
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

        if (_netMan.IsClient)
        {
            _transform.DetachParentToNull(uid, xform);
            return;
        }

    }

    /// <summary>
    /// Changes phantom's essense amount, if result is 0 tries to revive phantom
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="amount">Amount of gain/removed essense</param>
    /// <param name="component">Phantom component</param>
    /// <param name="allowDeath">Should it kill/revive phantom</param>
    /// <param name="regenCap">Set maximum allowed essense if result is more?</param>
    /// <returns>Is essense amount changed</returns>
    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, PhantomComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Essence + amount <= 0)
            return false;

        component.Essence += amount;

        if (regenCap)
            FixedPoint2.Min(component.Essence, component.EssenceRegenCap);

        _alerts.ShowAlert(uid, _proto.Index(component.EssenceCountAlert), (short) Math.Clamp(Math.Round(component.Essence.Float() / 10f), 0, 16));
        _alerts.ShowAlert(uid, _proto.Index(component.VesselCountAlert), (short) Math.Clamp(component.Vessels.Count, 0, 10));

        if (component.Essence <= 0)
        {
            if (!TryRevive(uid, component))
            {
                Spawn(component.SpawnOnDeathPrototype, Transform(uid).Coordinates);
                QueueDel(uid);
            }
        }
        return true;
    }

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
    /// When oath is accepted
    /// </summary>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    public void MakePuppet(EntityUid target, PhantomComponent component)
    {
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;
        if (!HasComp<VesselComponent>(target))
            return;

        EnsureComp<PhantomPuppetComponent>(target);
        component.CursedVessels.Add(target);
    }

    /// <summary>
    /// If there are any altars
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <returns>Is there are any altars</returns>
    public bool CheckAltars(EntityUid uid, PhantomComponent? component = null)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(uid, out var xform) || xform.MapUid == null)
            return false;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, 7f))
        {
            if (HasComp<AltarComponent>(ent))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Inserts target into helping hand container
    /// </summary>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    public void OnHelpingHandAccept(EntityUid target, PhantomComponent component)
    {
        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-helping-hand-toolate");
            _popup.PopupEntity(selfMessage, target, target);
            return;
        }
        StopHaunt(component.Owner, target, component);
        component.HelpingHandTimer = component.HelpingHandDuration;
        _container.Insert(target, component.HelpingHand, Transform(component.Owner));
        component.TransferringEntity = target;
    }

    /// <summary>
    /// All checks that shows if phantom able to use this ability
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="trgt">Target uid (nullable)</param>
    /// <param name="comp">Phantom component</param>
    /// <returns>Is phantom able to use this ability</returns>
    private bool TryUseAbility(EntityUid uid, EntityUid? trgt = null)
    {
        if (trgt == null)
        {
            if (CheckAltars(uid))
                return false;
            return true;
        }
        var target = trgt.Value;

        if (HasComp<ChaplainComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-ability-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, target, uid);
            return false;
        }

        if (HasComp<PhantomImmuneComponent>(target))
        {
            var selfMessage = Loc.GetString("phantom-ability-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, target, uid);
            return false;
        }

        if (_inventorySystem.TryGetSlotEntity(target, OuterClothingId, out var outerclothingitem) && HasComp<GrantPhantomProtectionComponent>(outerclothingitem))
        {
            var selfMessage = Loc.GetString("phantom-ability-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, target, uid);
            return false;
        }

        if (TryComp<ContainerManagerComponent>(target, out var containerManager))
        {
            foreach (var container in containerManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<GrantPhantomProtectionComponent>(entity, out var protection) && protection.WorkInHand)
                    {
                        var selfMessage = Loc.GetString("phantom-ability-fail-self", ("target", Identity.Entity(target, EntityManager)));
                        _popup.PopupEntity(selfMessage, target, uid);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Spawns ectoplasm on station when abilities limit is reached
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    private void UpdateEctoplasmSpawn(EntityUid uid, PhantomComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;
        if (!TryComp<AlertLevelComponent>(stationUid.Value, out var alert))
            return;

        component.UsedActionsBeforeEctoplasm += 1;

        if (alert.CurrentLevel == "green" && component.UsedActionsBeforeEctoplasm > 3)
        {
            var newCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(_random.NextFloat(5, 30)));

            Spawn("ADTPhantomEctoplasm", newCoords);

            component.UsedActionsBeforeEctoplasm = 0;
            return;
        }

        else if (alert.CurrentLevel == "blue" && component.UsedActionsBeforeEctoplasm > 2)
        {
            var newCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(_random.NextFloat(5, 30)));

            Spawn("ADTPhantomEctoplasm", newCoords);

            component.UsedActionsBeforeEctoplasm = 0;
            return;
        }

        else if (alert.CurrentLevel == "red" && component.UsedActionsBeforeEctoplasm > 1)
        {
            var newCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(_random.NextFloat(5, 30)));

            Spawn("ADTPhantomEctoplasm", newCoords);

            component.UsedActionsBeforeEctoplasm = 0;
            return;
        }

        else if (component.UsedActionsBeforeEctoplasm > 3)
        {
            var newCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(_random.NextFloat(5, 30)));

            Spawn("ADTPhantomEctoplasm", newCoords);

            component.UsedActionsBeforeEctoplasm = 0;
            return;
        }
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
            if (!TryComp<MetaDataComponent>(vessel, out var meta))
                continue;
            var netEnt = GetNetEntity(vessel);

            HumanoidCharacterAppearance hca = new();

            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.FacialHair, out var facialHair))
                if (facialHair.TryGetValue(0, out var marking))
                {
                    hca = hca.WithFacialHairStyleName(marking.MarkingId);
                    hca = hca.WithFacialHairColor(marking.MarkingColors.First());
                }
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Hair, out var hair))
                if (hair.TryGetValue(0, out var marking))
                {
                    hca = hca.WithHairStyleName(marking.MarkingId);
                    hca = hca.WithHairColor(marking.MarkingColors.First());
                }
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Head, out var head))
                hca = hca.WithMarkings(head);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.HeadSide, out var headSide))
                hca = hca.WithMarkings(headSide);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.HeadTop, out var headTop))
                hca = hca.WithMarkings(headTop);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Snout, out var snout))
                hca = hca.WithMarkings(snout);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Chest, out var chest))
                hca = hca.WithMarkings(chest);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Arms, out var arms))
                hca = hca.WithMarkings(arms);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Legs, out var legs))
                hca = hca.WithMarkings(legs);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var tail))
                hca = hca.WithMarkings(tail);
            if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Overlay, out var overlay))
                hca = hca.WithMarkings(overlay);

            hca = hca.WithSkinColor(humanoid.SkinColor);
            hca = hca.WithEyeColor(humanoid.EyeColor);
            //hca = hca.WithMarkings(humanoid.MarkingSet.Markings.);

            HumanoidCharacterProfile profile = new HumanoidCharacterProfile().WithCharacterAppearance(hca).WithSpecies(humanoid.Species);

            ev.Vessels.Add((netEnt, profile, meta.EntityName));
        }
        ev.Vessels.Sort();

        RaiseNetworkEvent(ev, uid);
    }
    #endregion

    #region Puppets
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

            _inventorySystem.TryUnequip(uid, GlovesId, true, true, false, inventory);
            _inventorySystem.TryEquip(uid, claws, GlovesId, true, true, false, inventory);
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

    #region Finale

    /// <summary>
    /// Raised when player pressed accept button in FinaleEui
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="type"></param>
    public void Finale(EntityUid uid, PhantomComponent component, PhantomFinaleType type)
    {
        component.FinalAbilityUsed = true;
        if (_proto.TryIndex<PhantomStylePrototype>(component.CurrentStyle, out var proto))
        {
            foreach (var action in component.CurrentActions)
            {

                if (action == null || TryPrototype(action.Value, out var prototype) || prototype == null)
                    continue;
                foreach (var lvl5action in proto.Lvl5Actions)
                {
                    if (prototype.ID == lvl5action)
                        _action.RemoveAction(uid, action);
                }
            }
        }

        switch (type)
        {
            case PhantomFinaleType.Nightmare:
                Nightmare(uid, component);
                break;
            case PhantomFinaleType.Tyrany:
                Tyrany(uid, component);
                break;
            case PhantomFinaleType.Oblivion:
                Oblivion(uid, component);
                break;
            case PhantomFinaleType.Deathmatch:
                Deathmatch(uid, component);
                break;
            case PhantomFinaleType.Help:
                Blessing(uid, component);
                break;
        }
    }

    public void Nightmare(EntityUid uid, PhantomComponent component)
    {
        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        var target = component.Holder;

        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;

        var ev = new PhantomNightmareEvent();
        RaiseLocalEvent(uid, ref ev);

        var list = new List<EntityUid>();
        foreach (var vessel in component.Vessels)
        {
            list.Add(vessel);
        }

        foreach (var item in list)
        {
            if (item == component.Holder)
                continue;

            var monster = Spawn(_random.Pick(component.NightmareMonsters), Transform(item).Coordinates);
            if (_mindSystem.TryGetMind(item, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, monster);
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), 500f);
            _damageableSystem.TryChangeDamage(item, damage);
        }

        _alertLevel.SetLevel(stationUid.Value, "delta", false, true, true, true);
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-nightmare-announcement"), Loc.GetString("phantom-announcer"), true, component.NightmareSound, Color.DarkCyan);
        _audio.PlayGlobal(component.NightmareSong, Filter.Broadcast(), true);
        EnsureComp<PhantomPuppetComponent>(target);
        component.CanHaunt = false;
        component.NightmareStarted = true;
        component.IgnoreLevels = true;
    }

    public void Tyrany(EntityUid uid, PhantomComponent component)
    {
        if (component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder-need");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var stationUid = _entitySystems.GetEntitySystem<StationSystem>().GetOwningStation(uid);
        if (stationUid == null)
            return;

        var ev = new PhantomTyranyEvent();
        RaiseLocalEvent(uid, ref ev);

        _alertLevel.SetLevel(stationUid.Value, "delta", false, false, true, true);
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-tyrany-announcement"), Loc.GetString("phantom-announcer"), true, component.TyranySound, Color.DarkCyan);
        _audio.PlayGlobal(component.TyranySong, Filter.Broadcast(), true);

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physicsSystem.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), fixtures);
            _physicsSystem.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
        }
        var visibility = EnsureComp<VisibilityComponent>(uid);

        _visibility.SetLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
        _visibility.RefreshVisibility(uid);

        component.IsCorporeal = true;

        var weapon = EnsureComp<MeleeWeaponComponent>(uid);
        weapon.Damage = new DamageSpecifier(_proto.Index(BruteDamageGroup), (FixedPoint2) 20);

        EnsureComp<CombatModeComponent>(uid);

        var list = new List<EntityUid>();
        foreach (var vessel in component.Vessels)
        {
            list.Add(vessel);
        }

        foreach (var item in list)
        {
            var light = Spawn("PseudoEntityPhantomLight", Transform(item).Coordinates);

            var targetXform = Transform(item);
            while (targetXform.ParentUid.IsValid())
            {
                if (targetXform.ParentUid == light)
                    return;

                targetXform = Transform(targetXform.ParentUid);
            }

            var xform = Transform(light);
            ContainerSystem.AttachParentToContainerOrGrid((light, xform));

            // If we didn't get to parent's container.
            if (xform.ParentUid != Transform(xform.ParentUid).ParentUid)
            {
                _transform.SetCoordinates(light, xform, new EntityCoordinates(item, Vector2.Zero), rotation: Angle.Zero);
            }
            _physicsSystem.SetLinearVelocity(light, Vector2.Zero);

            if (TryComp<PointLightComponent>(light, out var comp))
                Dirty(light, comp);
        }

        _status.TryAddStatusEffect<StunnedComponent>(uid, "Stun", TimeSpan.FromSeconds(3), false);
        component.CanHaunt = false;
        component.TyranyStarted = true;
    }

    public void Oblivion(EntityUid uid, PhantomComponent component)
    {
        if (component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder-need");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        foreach (var vessel in component.Vessels)
        {
            if (!HasComp<VesselComponent>(vessel))
                continue;

            var time = TimeSpan.FromSeconds(3);
            _status.TryAddStatusEffect<KnockedDownComponent>(vessel, "KnockedDown", time, true);
            _status.TryAddStatusEffect<StunnedComponent>(vessel, "Stun", time, true);

            if (_mindSystem.TryGetMind(vessel, out _, out var mind) && mind.Session != null)
            {
                _euiManager.OpenEui(new PhantomAmnesiaEui(), mind.Session);
                _audio.PlayGlobal(component.OblibionSound, mind.Session);
            }

            if (HasComp<PhantomPuppetComponent>(vessel))
                RemComp<PhantomPuppetComponent>(vessel);
        }

        var human = Spawn("ADTPhantomReincarnationAnim", Transform(uid).Coordinates);
        if (_mindSystem.TryGetMind(uid, out var mindId, out var selfMind) && selfMind.Session != null)
        {
            _mindSystem.TransferTo(mindId, human);
            var ev = new PhantomReincarnatedEvent();
            RaiseLocalEvent(uid, ref ev);
            QueueDel(uid);
            _euiManager.OpenEui(new PhantomAmnesiaEui(), selfMind.Session);
            _audio.PlayGlobal(component.OblivionSong, selfMind.Session);
        }
    }

    public void Deathmatch(EntityUid uid, PhantomComponent component)
    {
        if (component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder-need");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        foreach (var vessel in component.Vessels)
        {
            if (HasComp<PhantomPuppetComponent>(vessel))
                continue;
            var sword = Spawn("Claymore", Transform(vessel).Coordinates);
            if (TryComp<CuffableComponent>(uid, out var cuffs) && cuffs.Container.ContainedEntities.Count > 0)
            {
                if (!TryComp<HandcuffComponent>(cuffs.LastAddedCuffs, out var handcuffs) || cuffs.Container.ContainedEntities.Count > 0)
                    _cuffable.Uncuff(vessel, vessel, cuffs.LastAddedCuffs, cuffs, handcuffs);
            }
            if (_handsSystem.TryForcePickupAnyHand(vessel, sword))
                EnsureComp<UnremoveableComponent>(sword);

            var ev = new RejuvenateEvent();
            RaiseLocalEvent(vessel, ev);

            _damageableSystem.SetDamageModifierSetId(vessel, "Pretender");

            EnsureComp<ShowVesselIconsComponent>(vessel);

            if (_mindSystem.TryGetMind(vessel, out var mindId, out var mind) && mind.Session != null)
            {

                //_mindSystem.TryAddObjective(mindId, mind, "NotYet");
            }
        }
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-deathmatch-announcement"), Loc.GetString("phantom-announcer"), true, component.TyranySound, Color.DarkCyan);
        _audio.PlayGlobal(component.DeathmatchSound, Filter.Broadcast(), true);

        _audio.PlayGlobal(component.DeathmatchSong, Filter.Broadcast(), true);
        var human = Spawn("ADTPhantomReincarnationAnim", Transform(uid).Coordinates);
        if (_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) && selfMind.Session != null)
        {
            _mindSystem.TransferTo(selfMindId, human);
            var ev = new PhantomReincarnatedEvent();
            RaiseLocalEvent(uid, ref ev);
            QueueDel(uid);
        }
    }

    public void Blessing(EntityUid uid, PhantomComponent component)
    {
        if (component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder-need");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        var list = new List<EntityUid>();
        foreach (var vessel in component.Vessels)
        {
            list.Add(vessel);
        }
        foreach (var item in list)
        {
            RemComp<VesselComponent>(item);
            RemComp<PhantomPuppetComponent>(item);
        }
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-blessing-announcement"), colorOverride: Color.Gold, playSound: false);
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-blessing-second-announcement"), colorOverride: Color.Gold, playSound: false, sender: Loc.GetString("phantom-blessing-second-announcer"));
        _audio.PlayGlobal(component.HelpSound, Filter.Broadcast(), true);

        var human = Spawn("ADTPhantomReincarnationAnim", Transform(uid).Coordinates);
        if (_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) && selfMind.Session != null)
        {
            _mindSystem.TransferTo(selfMindId, human);
            var ev = new PhantomReincarnatedEvent();
            RaiseLocalEvent(uid, ref ev);
            QueueDel(uid);
            _audio.PlayGlobal(component.HelpSong, selfMind.Session);
        }
    }
    #endregion

    #region Holy Damage
    private void OnProjectileHit(EntityUid uid, HolyDamageComponent component, ref ProjectileHitEvent args)
    {
        if (TryComp<PhantomHolderComponent>(args.Target, out var holder))
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
            _damageableSystem.TryChangeDamage(holder.Phantom, damage);
            StopHaunt(holder.Phantom, args.Target);
        }

        if (HasComp<VesselComponent>(args.Target))
        {
            if (HasComp<PhantomPuppetComponent>(args.Target))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
            else
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }
        if (TryComp<HereticComponent>(args.Target, out var heretic) && heretic.PathStage > 3)
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.Damage);
            _damageableSystem.TryChangeDamage(args.Target, damage);
        }
    }

    private void OnThrowHit(EntityUid uid, HolyDamageComponent component, ThrowDoHitEvent args)
    {
        if (TryComp<PhantomHolderComponent>(args.Target, out var holder))
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
            _damageableSystem.TryChangeDamage(holder.Phantom, damage);
            StopHaunt(holder.Phantom, args.Target);
        }

        if (HasComp<VesselComponent>(args.Target))
        {
            if (HasComp<PhantomPuppetComponent>(args.Target))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
            else
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }
        if (TryComp<HereticComponent>(args.Target, out var heretic) && heretic.PathStage > 3)
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.Damage);
            _damageableSystem.TryChangeDamage(args.Target, damage);
        }
    }

    private void OnMeleeHit(EntityUid uid, HolyDamageComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            component.Damage <= 0f)
        {
            return;
        }

        foreach (var ent in args.HitEntities)
        {
            if (HasComp<RevenantComponent>(ent) || HasComp<PhantomComponent>(ent))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
                _damageableSystem.TryChangeDamage(ent, damage);

                var time = TimeSpan.FromSeconds(2);
                _status.TryAddStatusEffect<KnockedDownComponent>(args.User, "KnockedDown", time, false);
                _status.TryAddStatusEffect<StunnedComponent>(args.User, "Stun", time, false);
            }
            if (TryComp<PhantomHolderComponent>(ent, out var holder))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
                _damageableSystem.TryChangeDamage(holder.Phantom, damage);
                StopHaunt(holder.Phantom, ent);
            }

            if (TryComp<HereticComponent>(ent, out var heretic) && heretic.PathStage > 3)
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.Damage);
                _damageableSystem.TryChangeDamage(ent, damage);
            }

            if (HasComp<VesselComponent>(ent))
            {
                if (HasComp<PhantomPuppetComponent>(ent))
                {
                    var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                    _damageableSystem.TryChangeDamage(ent, damage);
                }
                else
                {
                    var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                    _damageableSystem.TryChangeDamage(ent, damage);
                }
            }
        }
    }
    #endregion
}
