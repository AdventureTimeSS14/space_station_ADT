using Content.Shared.Actions;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Server.Power.EntitySystems;
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
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye;
using Content.Server.Light.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Bible.Components;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Server.EUI;
using Content.Server.ADT.Hallucinations;
using Content.Server.AlertLevel;
using Content.Shared.ADT.Controlled;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.CombatMode;
using Content.Server.Cuffs;
using Robust.Server.Player;
using Content.Shared.Ghost;
using Content.Server.Hands.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.GhostInteractions;
using Content.Shared.Revenant.Components;
using Content.Server.Singularity.Events;
using Content.Shared.ADT.MindShield;
using Robust.Shared.Audio;

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
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ApcSystem _apcSystem = default!;
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
    #endregion

    public static SoundSpecifier NightmareSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/tyrany-nightmare.ogg");
    public static SoundSpecifier TyranySound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/tyrany-nightmare.ogg");
    public static SoundSpecifier DeathmatchSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-deathmatch.ogg");
    public static SoundSpecifier OblibionSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-oblivion.ogg");
    public static SoundSpecifier NightmareSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/nightmare.ogg");
    public static SoundSpecifier TyranySong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/tyrany.ogg");
    public static SoundSpecifier OblivionSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/oblivion.ogg");
    public static SoundSpecifier DeathmatchSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/deathmatch.ogg");
    public static SoundSpecifier HelpSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/help.ogg");
    public static SoundSpecifier HelpSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-help.ogg");

    public override void Initialize()
    {
        base.Initialize();

        InitializeHaunting();
        InitializeVessels();
        InitializeControlAbilities();
        InitializeHarmAbilities();
        InitializeHelpAbilities();

        // Startup
        SubscribeLocalEvent<PhantomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PhantomComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PhantomComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PhantomComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<PhantomComponent, StatusEffectEndedEvent>(OnStatusEnded);

        // Radial
        SubscribeNetworkEvent<SelectPhantomStyleEvent>(OnSelectStyle);
        SubscribeNetworkEvent<SelectPhantomFreedomEvent>(OnSelectFreedom);

        // Finales
        SubscribeLocalEvent<PhantomComponent, NightmareFinaleActionEvent>(OnNightmare);
        SubscribeLocalEvent<PhantomComponent, TyranyFinaleActionEvent>(OnTyrany);

        // Other
        SubscribeLocalEvent<PhantomComponent, AlternativeSpeechEvent>(OnTrySpeak);
        SubscribeLocalEvent<PhantomComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<PhantomComponent, RefreshPhantomLevelEvent>(OnLevelChanged);
        SubscribeLocalEvent<PhantomComponent, EventHorizonAttemptConsumeEntityEvent>(OnSinguloConsumeAttempt);

        // IDK why the fuck this is not working in another file
        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";

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

    private void OnMapInit(EntityUid uid, PhantomComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.PhantomHauntActionEntity, component.PhantomHauntAction);
        _action.AddAction(uid, ref component.PhantomMakeVesselActionEntity, component.PhantomMakeVesselAction);
        _action.AddAction(uid, ref component.PhantomStyleActionEntity, component.PhantomStyleAction);
        _action.AddAction(uid, ref component.PhantomHauntVesselActionEntity, component.PhantomHauntVesselAction);
        SelectStyle(uid, component, component.CurrentStyle, true);

        if (TryComp<EyeComponent>(uid, out var eyeComponent))
            _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int)VisibilityFlags.PhantomVessel, eyeComponent);

        component.HelpingHand = _container.EnsureContainer<Container>(uid, "HelpingHand");
    }

    private void OnStartup(EntityUid uid, PhantomComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, PhantomVisuals.Haunting, false);
        _appearance.SetData(uid, PhantomVisuals.Stunned, false);
        _appearance.SetData(uid, PhantomVisuals.Corporeal, false);
        ChangeEssenceAmount(uid, 0, component);
    }

    private void OnShutdown(EntityUid uid, PhantomComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.PhantomHauntActionEntity);
        _action.RemoveAction(uid, component.PhantomMakeVesselActionEntity);
        _action.RemoveAction(uid, component.PhantomStyleActionEntity);
        _action.RemoveAction(uid, component.PhantomHauntVesselActionEntity);

        foreach (var action in component.CurrentActions)
        {
            _action.RemoveAction(uid, action);
            if (action != null)
                QueueDel(action.Value);
        }
        if (TryComp<EyeComponent>(uid, out var eyeComponent))
            _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | ~(int)VisibilityFlags.PhantomVessel, eyeComponent);
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
                    ("defaultFont", "Default"),
                    ("defaultSize", 12),
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

    private void OnLevelChanged(EntityUid uid, PhantomComponent component, ref RefreshPhantomLevelEvent args)
    {
        SelectStyle(uid, component, component.CurrentStyle, true);
        _alerts.ShowAlert(uid, _proto.Index(component.VesselCountAlert), (short) Math.Clamp(component.Vessels.Count, 0, 10));
    }

    private void OnSinguloConsumeAttempt(EntityUid uid, PhantomComponent component, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        args.Cancelled = true;
    }
    #endregion

    #region Raised Not By Events
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
            if (!CheckAltars(vessel, component) && CheckProtection(uid, vessel))
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
            Haunt(uid, randomVessel);
        }
        else
        {
            StopHaunt(uid, component.Holder, component);
            Haunt(uid, randomVessel);
        }
        return true;
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

    public bool TryUseAbility(EntityUid uid, IPhantomAbility args, EntityUid? target = null)
    {
        if (target.HasValue)
        {
            switch (args.MsAllowance)
            {
                case IPhantomAbility.MindshieldAllowance.Any:
                    break;
                case IPhantomAbility.MindshieldAllowance.Malfunctioning:
                    if (HasComp<MindShieldComponent>(target.Value) && !HasComp<MindShieldMalfunctioningComponent>(target.Value))
                    {
                        var selfMessage = Loc.GetString("phantom-ability-fail-mindshield", ("target", Identity.Entity(target.Value, EntityManager)));
                        _popup.PopupEntity(selfMessage, uid, uid);
                        return false;
                    }
                    break;
                case IPhantomAbility.MindshieldAllowance.NoMindshield:
                    if (HasComp<MindShieldComponent>(target.Value))
                    {
                        var selfMessage = Loc.GetString("phantom-ability-fail-mindshield", ("target", Identity.Entity(target.Value, EntityManager)));
                        _popup.PopupEntity(selfMessage, uid, uid);
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        if (!CheckProtection(uid, target))
            return false;

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
            _audio.PlayGlobal(args.Sound, session);

        return true;
    }

    /// <summary>
    /// All checks that shows if phantom able to use this ability
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="trgt">Target uid (nullable)</param>
    /// <param name="comp">Phantom component</param>
    /// <returns>Is phantom able to use this ability</returns>
    private bool CheckProtection(EntityUid uid, EntityUid? trgt = null)
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

        if (_inventorySystem.TryGetSlotEntity(target, "outerClothing", out var outerclothingitem) && HasComp<GrantPhantomProtectionComponent>(outerclothingitem))
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

        if (component.UsedActionsBeforeEctoplasm >= 3)
        {
            var newCoords = Transform(uid).Coordinates.Offset(_random.NextVector2(_random.NextFloat(5, 30)));
            SpawnAtPosition("ADTPhantomEctoplasm", newCoords);

            component.UsedActionsBeforeEctoplasm = 0;
            return;
        }
    }

    #endregion

    #region Finale
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

        if (!CheckProtection(uid, target))
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
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-nightmare-announcement"), Loc.GetString("phantom-announcer"), true, NightmareSound, Color.DarkCyan);
        _audio.PlayGlobal(NightmareSong, Filter.Broadcast(), true);
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
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-tyrany-announcement"), Loc.GetString("phantom-announcer"), true, TyranySound, Color.DarkCyan);
        _audio.PlayGlobal(TyranySong, Filter.Broadcast(), true);

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
            _container.AttachParentToContainerOrGrid((light, xform));

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
                _audio.PlayGlobal(OblibionSound, mind.Session);
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
            _audio.PlayGlobal(OblivionSong, selfMind.Session);
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

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("phantom-deathmatch-announcement"), Loc.GetString("phantom-announcer"), true, TyranySound, Color.DarkCyan);
        _audio.PlayGlobal(DeathmatchSound, Filter.Broadcast(), true);

        _audio.PlayGlobal(DeathmatchSong, Filter.Broadcast(), true);
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
        _audio.PlayGlobal(HelpSound, Filter.Broadcast(), true);

        var human = Spawn("ADTPhantomReincarnationAnim", Transform(uid).Coordinates);
        if (_mindSystem.TryGetMind(uid, out var selfMindId, out var selfMind) && selfMind.Session != null)
        {
            _mindSystem.TransferTo(selfMindId, human);
            var ev = new PhantomReincarnatedEvent();
            RaiseLocalEvent(uid, ref ev);
            QueueDel(uid);
            _audio.PlayGlobal(HelpSong, selfMind.Session);
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
