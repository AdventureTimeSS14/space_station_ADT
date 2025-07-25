using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared.Humanoid; // ADT-Changeling-Tweak
//ADT-Geras-Tweak-Start
using Content.Shared.ADT.Language;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Corvax.TTS;
using Content.Server.Speech.Components;
using Content.Server.Corvax.Speech.Components;
using Content.Shared.Speech.Components;
using Content.Server._CorvaxNext.Speech.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Speech.Muting;
using Content.Shared.ADT.Traits;
using Content.Shared.Storage.Components;
//ADT-Geras-Tweak-End
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Forensics.Components; // ADT-Changeling-Tweak
using Content.Shared.Mindshield.Components; // ADT-Changeling-Tweak
using Robust.Shared.Serialization.Manager;
using Content.Shared.DetailExaminable; // ADT-Changeling-Tweak

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly FollowerSystem _follow = default!; // goob edit

    [Dependency] private readonly ISerializationManager _serialization = default!; // ADT-Changeling-Tweak
    private const string RevertPolymorphId = "ActionRevertPolymorph";

    public override void Initialize()
    {
        SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PolymorphedEntityComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
        SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);

        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);
        SubscribeLocalEvent<PolymorphedEntityComponent, DestructionEventArgs>(OnDestruction);

        InitializeMap();
        InitializeTrigger();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PolymorphedEntityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Time += frameTime;

            if (comp.Configuration.Duration != null && comp.Time >= comp.Configuration.Duration)
            {
                Revert((uid, comp));
                continue;
            }

            if (!TryComp<MobStateComponent>(uid, out var mob))
                continue;

            if (comp.Configuration.RevertOnDeath && _mobState.IsDead(uid, mob) ||
                comp.Configuration.RevertOnCrit && _mobState.IsIncapacitated(uid, mob))
            {
                Revert((uid, comp));
            }
        }

        UpdateTrigger();
    }

    private void OnComponentStartup(Entity<PolymorphableComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.InnatePolymorphs != null)
        {
            foreach (var morph in ent.Comp.InnatePolymorphs)
            {
                CreatePolymorphAction(morph, ent);
            }
        }
    }

    private void OnMapInit(Entity<PolymorphedEntityComponent> ent, ref MapInitEvent args)
    {
        var (uid, component) = ent;
        if (component.Configuration.Forced)
            return;

        if (_actions.AddAction(uid, ref component.Action, out var action, RevertPolymorphId))
        {
            action.EntityIcon = component.Parent;
            action.UseDelay = TimeSpan.FromSeconds(component.Configuration.Delay);
        }
    }

    private void OnPolymorphActionEvent(Entity<PolymorphableComponent> ent, ref PolymorphActionEvent args)
    {
        if (!_proto.TryIndex(args.ProtoId, out var prototype) || args.Handled)
            return;

        PolymorphEntity(ent, prototype.Configuration);

        args.Handled = true;
    }

    private void OnRevertPolymorphActionEvent(Entity<PolymorphedEntityComponent> ent,
        ref RevertPolymorphActionEvent args)
    {
        Revert((ent, ent));
    }

    private void OnBeforeFullyEaten(Entity<PolymorphedEntityComponent> ent, ref BeforeFullyEatenEvent args)
    {
        var (_, comp) = ent;
        if (comp.Configuration.RevertOnEat)
        {
            args.Cancel();
            Revert((ent, ent));
        }
    }

    private void OnBeforeFullySliced(Entity<PolymorphedEntityComponent> ent, ref BeforeFullySlicedEvent args)
    {
        var (_, comp) = ent;
        if (comp.Configuration.RevertOnEat)
        {
            args.Cancel();
            Revert((ent, ent));
        }
    }

    /// <summary>
    /// It is possible to be polymorphed into an entity that can't "die", but is instead
    /// destroyed. This handler ensures that destruction is treated like death.
    /// </summary>
    private void OnDestruction(Entity<PolymorphedEntityComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Configuration.RevertOnDeath)
        {
            Revert((ent, ent));
        }
    }

    /// <summary>
    /// Polymorphs the target entity into the specific polymorph prototype
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="protoId">The id of the polymorph prototype</param>
    public EntityUid? PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
        var config = _proto.Index(protoId).Configuration;
        return PolymorphEntity(uid, config);
    }

    /// <summary>
    /// Polymorphs the target entity into another
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="configuration">Polymorph data</param>
    /// <returns></returns>
        public EntityUid? PolymorphEntity(EntityUid uid, PolymorphConfiguration configuration)
    {
        //ADT-Geras-Tweak-Start
        if (configuration.CanNotPolymorphInStorage && HasComp<InsideEntityStorageComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("polymorph-in-storage-forbidden"), uid, uid);
            return null;
        }
        //ADT-Geras-Tweak-End

        // if it's already morphed, don't allow it again with this condition active.
        if (!configuration.AllowRepeatedMorphs && HasComp<PolymorphedEntityComponent>(uid))
            return null;

        // If this polymorph has a cooldown, check if that amount of time has passed since the
        // last polymorph ended.
        if (TryComp<PolymorphableComponent>(uid, out var polymorphableComponent) &&
            polymorphableComponent.LastPolymorphEnd != null &&
            _gameTiming.CurTime < polymorphableComponent.LastPolymorphEnd + configuration.Cooldown)
            return null;

        // mostly just for vehicles
        _buckle.TryUnbuckle(uid, uid, true);

        var targetTransformComp = Transform(uid);

        if (configuration.PolymorphSound != null)
            _audio.PlayPvs(configuration.PolymorphSound, targetTransformComp.Coordinates);

        var child = Spawn(configuration.Entity, _transform.GetMapCoordinates(uid, targetTransformComp), rotation: _transform.GetWorldRotation(uid));

        if (configuration.PolymorphPopup != null)
            _popup.PopupEntity(Loc.GetString(configuration.PolymorphPopup,
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(child, EntityManager))),
                child);

        MakeSentientCommand.MakeSentient(child, EntityManager);

        var polymorphedComp = _compFact.GetComponent<PolymorphedEntityComponent>();
        polymorphedComp.Parent = uid;
        polymorphedComp.Configuration = configuration;
        AddComp(child, polymorphedComp);

        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        if (_container.TryGetContainingContainer((uid, targetTransformComp, null), out var cont))
            _container.Insert(child, cont);

        //Transfers all damage from the original to the new one
        if (configuration.TransferDamage &&
            TryComp<DamageableComponent>(child, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, child, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage(child, damageParent, damage);
        }

        if (configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, child);
            foreach (var hand in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, hand, checkActionBlocker: false);
                _hands.TryPickupAnyHand(child, hand);
            }
        }
        else if (configuration.Inventory == PolymorphInventoryChange.Drop)
        {
            if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(uid, slot.ID, true, true);
                }
            }

            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
            }
        }

        if (configuration.TransferName && TryComp(uid, out MetaDataComponent? targetMeta))
            _metaData.SetEntityName(child, targetMeta.EntityName);

        // ADT-Geras-Tweak-Start
        if (configuration.TransferLanguageSpeaker && TryComp<LanguageSpeakerComponent>(uid, out var originalLangComp))
        {
            var childLangComp = EnsureComp<LanguageSpeakerComponent>(child);
            childLangComp.Languages = new Dictionary<string, LanguageKnowledge>(originalLangComp.Languages);
            childLangComp.CurrentLanguage = originalLangComp.CurrentLanguage;
        }

        if (configuration.TransferTTS && TryComp<TTSComponent>(uid, out var originalTTSComp))
        {
           var childTTSComp = EnsureComp<TTSComponent>(child);
           childTTSComp.VoicePrototypeId = originalTTSComp.VoicePrototypeId;
        }

        if (configuration.TransferSpeechBarks && TryComp<SpeechBarksComponent>(uid, out var originalBarksComp))
        {
            var childBarksComp = EnsureComp<SpeechBarksComponent>(child);
            childBarksComp.Data = originalBarksComp.Data;
        }

        if (configuration.TransferAccents)
        {
            var accentComponents = new List<Type>
            {
                typeof(AccentlessComponent),
                typeof(BackwardsAccentComponent),
                typeof(BarkAccentComponent),
                typeof(BleatingAccentComponent),
                typeof(DamagedSiliconAccentComponent),
                typeof(DeutschAccentComponent),
                typeof(FrenchAccentComponent),
                typeof(GermanAccentComponent),
                typeof(GrowlingAccentComponent),
                typeof(LizardAccentComponent),
                typeof(MobsterAccentComponent),
                typeof(MonkeyAccentComponent),
                typeof(MothAccentComponent),
                typeof(MumbleAccentComponent),
                typeof(NyaAccentComponent),
                typeof(OwOAccentComponent),
                typeof(ParrotAccentComponent),
                typeof(PirateAccentComponent),
                typeof(ReplacementAccentComponent),
                typeof(ResomiAccentComponent),
                typeof(RoarAccentComponent),
                typeof(RussianAccentComponent),
                typeof(ScrambledAccentComponent),
                typeof(SkeletonAccentComponent),
                typeof(SlurredAccentComponent),
                typeof(SouthernAccentComponent),
                typeof(SpanishAccentComponent),
                typeof(StutteringAccentComponent),
                typeof(VoxAccentComponent),
                typeof(FrontalLispComponent)
            };

            foreach (var accentType in accentComponents)
            {
                if (EntityManager.HasComponent(uid, accentType))
                {
                    var originalAccentComp = EntityManager.GetComponent(uid, accentType);
                    var childAccentComp = (Component)_serialization.CreateCopy(originalAccentComp, notNullableOverride: true);
            EntityManager.AddComponent(child, childAccentComp);
                }
            }
        }

        if (configuration.TransferQuirks)
        {
            var quirkComponents = new List<Type> //Вроде бы все добавил???
            {
                typeof(PacifiedComponent),
                typeof(LightweightDrunkComponent),
                typeof(SnoringComponent),
                typeof(BlindableComponent),
                typeof(PermanentBlindnessComponent),
                typeof(BlurryVisionComponent),
                typeof(TemporaryBlindnessComponent),
                typeof(UncloneableComponent),
                typeof(NarcolepsyComponent),
                typeof(UnrevivableComponent),
                typeof(MutedComponent),
                typeof(ParacusiaComponent),
                typeof(PainNumbnessComponent),
                typeof(HemophiliaComponent),
                typeof(DeafTraitComponent),
                typeof(MonochromacyComponent),
                typeof(FrailComponent),
                typeof(SoftWalkComponent),
                typeof(FreerunningComponent),
                typeof(SprinterComponent),
                typeof(FastLockersComponent),
                typeof(HardThrowerComponent),
                typeof(FoodConsumptionSpeedModifierComponent),
                typeof(DrunkenResilienceComponent)
            };

            foreach (var quirkType in quirkComponents)
            {
                if (EntityManager.HasComponent(uid, quirkType))
                {
                    var originalQuirkComp = EntityManager.GetComponent(uid, quirkType);
                    var childQuirkComp = (Component)_serialization.CreateCopy(originalQuirkComp, notNullableOverride: true);
                    EntityManager.AddComponent(child, childQuirkComp);
                }
            }
        }
        // ADT-Geras-Tweak-End

        if (configuration.TransferHumanoidAppearance)
        {
            _humanoid.CloneAppearance(uid, child);
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);
        SendToPausedMap(uid, targetTransformComp); // ADT-Tweak

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, child, false);
        RaiseLocalEvent(uid, ref ev);

        // visual effect spawn
        if (configuration.EffectProto != null)
            SpawnAttachedTo(configuration.EffectProto, child.ToCoordinates());

        return child;
    }

    /// <summary>
    /// Polymorphs the target entity into an exact copy of the given PolymorphHumanoidData
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="data">The humanoid data</param>
    public EntityUid? PolymorphEntityAsHumanoid(EntityUid uid, PolymorphHumanoidData data)
    {
        var targetTransformComp = Transform(uid);
        var child = data.EntityUid;

        RetrievePausedEntity(uid, child);

        if (TryComp<HumanoidAppearanceComponent>(child, out var humanoidAppearance))
            _humanoid.SetAppearance(data.HumanoidAppearanceComponent, humanoidAppearance);

        if (TryComp<DnaComponent>(child, out var dnaComp))
        {
            dnaComp.DNA = data.DNA;
            Dirty(child, dnaComp);
        }

        //Transfers all damage from the original to the new one
        if (TryComp<DamageableComponent>(child, out var damageParent)
            && _mobThreshold.GetScaledDamage(uid, child, out var damage)
            && damage != null)
        {
            _damageable.SetDamage(child, damageParent, damage);
        }

        _inventory.TransferEntityInventories(uid, child); // transfer the inventory all the time
        foreach (var hand in _hands.EnumerateHeld(uid))
        {
            if (!_hands.TryPickupAnyHand(child, hand))
                _hands.TryDrop(uid, hand, checkActionBlocker: false);
        }
        // ADT-Changeling-Tweak-End
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        EnsurePausedMap();
        if (PausedMap != null)
            _transform.SetParent(uid, targetTransformComp, PausedMap.Value);

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, child, false);
        RaiseLocalEvent(uid, ref ev);

        // goob edit
        if (TryComp<FollowedComponent>(uid, out var followed))
            foreach (var f in followed.Following)
            {
                _follow.StopFollowingEntity(f, uid);
                _follow.StartFollowingEntity(f, child);
            }
        // goob edit end

        return child;
    }
    // ADT-Changeling-Tweak-Start
    /// <summary>
    /// Sends the given entity to a pauses map
    /// </summary>
    public void SendToPausedMap(EntityUid uid, TransformComponent transform)
    {
        //Ensures a map to banish the entity to
        EnsurePausedMap();
        if (PausedMap != null)
            _transform.SetParent(uid, transform, PausedMap.Value);
    }

    /// <summary>
    /// Retrieves a paused entity (target) at the user's position
    /// </summary>
    private void RetrievePausedEntity(EntityUid user, EntityUid target)
    {
        if (Deleted(user))
            return;
        if (Deleted(target))
            return;

        var targetTransform = Transform(target);
        var userTransform = Transform(user);

        _transform.SetParent(target, targetTransform, user);
        targetTransform.Coordinates = userTransform.Coordinates;
        targetTransform.LocalRotation = userTransform.LocalRotation;

        if (_container.TryGetContainingContainer(user, out var cont))
            _container.Insert(target, cont);
    }
    // ADT-Changeling-Tweak-End

    /// <summary>
    /// Reverts a polymorphed entity back into its original form
    /// </summary>
    /// <param name="uid">The entityuid of the entity being reverted</param>
    /// <param name="component"></param>
    public EntityUid? Revert(Entity<PolymorphedEntityComponent?> ent)
    {
        var (uid, component) = ent;
        if (!Resolve(ent, ref component))
            return null;

        //ADT-Geras-Tweak-Start
        if (component.Configuration.CanNotPolymorphInStorage && HasComp<InsideEntityStorageComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("revert-in-storage-forbidden"), uid, uid);
            return null;
        }
        //ADT-Geras-Tweak-End

        if (Deleted(uid))
            return null;

        var parent = component.Parent;
        if (Deleted(parent))
            return null;

        var uidXform = Transform(uid);
        var parentXform = Transform(parent);

        if (component.Configuration.ExitPolymorphSound != null)
            _audio.PlayPvs(component.Configuration.ExitPolymorphSound, uidXform.Coordinates);

        _transform.SetParent(parent, parentXform, uidXform.ParentUid);
        _transform.SetCoordinates(parent, parentXform, uidXform.Coordinates, uidXform.LocalRotation);

        if (component.Configuration.TransferDamage &&
            TryComp<DamageableComponent>(parent, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, parent, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage(parent, damageParent, damage);
        }

        if (component.Configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, parent);
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
                _hands.TryPickupAnyHand(parent, held, checkActionBlocker: false);
            }
        }
        else if (component.Configuration.Inventory == PolymorphInventoryChange.Drop)
        {
            if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(uid, slot.ID);
                }
            }

            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
            }
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, parent, mind: mind);

        if (TryComp<PolymorphableComponent>(parent, out var polymorphableComponent))
            polymorphableComponent.LastPolymorphEnd = _gameTiming.CurTime;

        // if an item polymorph was picked up, put it back down after reverting
        _transform.AttachToGridOrMap(parent, parentXform);

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, parent, true);
        RaiseLocalEvent(uid, ref ev);

        // visual effect spawn
        if (component.Configuration.EffectProto != null)
            SpawnAttachedTo(component.Configuration.EffectProto, parent.ToCoordinates());

        if (component.Configuration.ExitPolymorphPopup != null)
            _popup.PopupEntity(Loc.GetString(component.Configuration.ExitPolymorphPopup,
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(parent, EntityManager))),
                parent);
        QueueDel(uid);

        // goob edit
        if (TryComp<FollowedComponent>(uid, out var followed))
            foreach (var f in followed.Following)
            {
                _follow.StopFollowingEntity(f, uid);
                _follow.StartFollowingEntity(f, parent);
            }
        // goob edit end

        return parent;
    }

    /// <summary>
    /// Creates a sidebar action for an entity to be able to polymorph at will
    /// </summary>
    /// <param name="id">The string of the id of the polymorph action</param>
    /// <param name="target">The entity that will be gaining the action</param>
    public void CreatePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        target.Comp.PolymorphActions ??= new();
        if (target.Comp.PolymorphActions.ContainsKey(id))
            return;

        if (!_proto.TryIndex(id, out var polyProto))
            return;

        var entProto = _proto.Index(polyProto.Configuration.Entity);

        EntityUid? actionId = default!;
        if (!_actions.AddAction(target, ref actionId, RevertPolymorphId, target))
            return;

        target.Comp.PolymorphActions.Add(id, actionId.Value);

        var metaDataCache = MetaData(actionId.Value);
        _metaData.SetEntityName(actionId.Value, Loc.GetString("polymorph-self-action-name", ("target", entProto.Name)), metaDataCache);
        _metaData.SetEntityDescription(actionId.Value, Loc.GetString("polymorph-self-action-description", ("target", entProto.Name)), metaDataCache);

        if (!_actions.TryGetActionData(actionId, out var baseAction))
            return;

        baseAction.Icon = new SpriteSpecifier.EntityPrototype(polyProto.Configuration.Entity);
        if (baseAction is InstantActionComponent action)
            action.Event = new PolymorphActionEvent(id);
    }

    public void RemovePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        if (target.Comp.PolymorphActions == null)
            return;

        if (target.Comp.PolymorphActions.TryGetValue(id, out var val))
            _actions.RemoveAction(target, val);
    }

    // ADT-Changeling-Tweak-Start
    /// <summary>
    /// Registers PolymorphHumanoidData from an EntityUid, provived they have all the needed components
    /// </summary>
    /// <param name="source">The source that the humanoid data is referencing from</param>
    public PolymorphHumanoidData? TryRegisterPolymorphHumanoidData(EntityUid source)
    {
        var newHumanoidData = new PolymorphHumanoidData();

        if (!TryComp<MetaDataComponent>(source, out var targetMeta))
            return null;
        if (!TryPrototype(source, out var prototype, targetMeta))
            return null;
        if (!TryComp<DnaComponent>(source, out var dnaComp))
            return null;
        if (!TryComp<HumanoidAppearanceComponent>(source, out var targetHumanoidAppearance))
            return null;


        newHumanoidData.EntityPrototype = prototype;
        newHumanoidData.MetaDataComponent = targetMeta;
        newHumanoidData.HumanoidAppearanceComponent = _serialization.CreateCopy(targetHumanoidAppearance, notNullableOverride: true);
        if (dnaComp.DNA != null)
            newHumanoidData.DNA = dnaComp.DNA;

        var targetTransformComp = Transform(source);

        var newEntityUid = Spawn(newHumanoidData.EntityPrototype.ID, targetTransformComp.Coordinates);
        var newEntityUidTransformComp = Transform(newEntityUid);

        if (TryComp(source, out MindShieldComponent? mindshieldComp)) // copy over mindshield status
        {
            var copiedMindshieldComp = (Component) _serialization.CreateCopy(mindshieldComp, notNullableOverride: true);
            EntityManager.AddComponent(newEntityUid, copiedMindshieldComp);
        }
        if (TryComp<DetailExaminableComponent>(source, out var desc))
        {
            var newDesc = EnsureComp<DetailExaminableComponent>(newEntityUid);
            newDesc.Content = desc.Content;
        }

        SendToPausedMap(newEntityUid, newEntityUidTransformComp);

        newHumanoidData.EntityUid = newEntityUid;
        _metaData.SetEntityName(newEntityUid, targetMeta.EntityName);

        return newHumanoidData;
    }

    /// <summary>
    /// Registers PolymorphHumanoidData from an EntityUid, provived they have all the needed components. This allows you to add a uid as the HumanoidData's EntityUid variable. Does not send the given uid to the pause map.
    /// </summary>
    /// <param name="source">The source that the humanoid data is referencing from</param>
    /// <param name="uid">The uid that will become the newHumanoidData's EntityUid</param>
    public PolymorphHumanoidData? TryRegisterPolymorphHumanoidData(EntityUid source, EntityUid uid)
    {
        var newHumanoidData = new PolymorphHumanoidData();

        if (!TryComp<MetaDataComponent>(source, out var targetMeta))
            return null;
        if (!TryPrototype(source, out var prototype, targetMeta))
            return null;
        if (!TryComp<DnaComponent>(source, out var dnaComp))
            return null;
        if (!TryComp<HumanoidAppearanceComponent>(source, out var targetHumanoidAppearance))
            return null;

        newHumanoidData.EntityPrototype = prototype;
        newHumanoidData.MetaDataComponent = targetMeta;
        newHumanoidData.HumanoidAppearanceComponent = _serialization.CreateCopy(targetHumanoidAppearance, notNullableOverride: true);
        if (dnaComp.DNA != null)
            newHumanoidData.DNA = dnaComp.DNA;
        newHumanoidData.EntityUid = uid;

        return newHumanoidData;
    }

    public PolymorphHumanoidData CopyPolymorphHumanoidData(PolymorphHumanoidData data)
    {
        var newHumanoidData = new PolymorphHumanoidData();
        var ent = Spawn(data.EntityPrototype.ID);
        SendToPausedMap(ent, Transform(ent));

        newHumanoidData.EntityPrototype = data.EntityPrototype;
        newHumanoidData.MetaDataComponent = data.MetaDataComponent;
        newHumanoidData.HumanoidAppearanceComponent = _serialization.CreateCopy(data.HumanoidAppearanceComponent, notNullableOverride: true);;
        newHumanoidData.DNA = data.DNA;
        newHumanoidData.EntityUid = ent;
        _metaData.SetEntityName(ent, data.MetaDataComponent.EntityName);
        return newHumanoidData;
    }
    // ADT-Changeling-Tweak-End
}

// goob edit
public sealed partial class PolymorphRevertEvent : EntityEventArgs { }
