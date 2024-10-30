using Content.Server.Actions;
using Content.Shared.Inventory;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Server.Traitor.Uplink;
using Content.Server.Body.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Server.Polymorph.Systems;
using Content.Server.Flash;
using Content.Shared.Polymorph;
using Content.Server.Forensics;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Stealth.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tag;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Server.Stunnable;
using Content.Shared.Mind;
using Robust.Shared.Player;
using System.Linq;
using Content.Shared.Preferences;
using Content.Server.Humanoid;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Content.Server.Hands.Systems;
using Content.Server.Body.Systems;
using Robust.Shared.Audio.Systems;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Cuffs;
using Robust.Shared.Timing;
using Content.Server.ADT.Hallucinations;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Mobs;
using Content.Server.Stealth;
using Content.Server.ADT.Store;
using Content.Shared.Examine;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    #region Dependency
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly FlashSystem _flashSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly GibbingSystem _gib = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<ChangelingComponent, ActionBoughtEvent>(OnActionBought);
        SubscribeLocalEvent<ChangelingComponent, ChangelingRefreshEvent>(OnRefresh);

        SubscribeLocalEvent<ChangelingComponent, ChangelingEvolutionMenuActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, ChangelingCycleDNAActionEvent>(OnCycleDNA);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransform);

        SubscribeNetworkEvent<SelectChangelingFormEvent>(OnSelectChangelingForm);

        InitializeUsefulAbilities();
        InitializeCombatAbilities();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var uid, out var ling))
        {
            ling.Accumulator += frameTime;

            if (ling.Accumulator <= 1)
                continue;
            ling.Accumulator -= 1;

            if (_mobState.IsDead(uid)) // if ling is dead dont regenerate chemicals
                continue;

            if (ling.Chemicals < ling.MaxChemicals)
                ChangeChemicalsAmount(uid, ling.ChemicalsPerSecond, ling, regenCap: true);

            if (ling.MusclesActive)
                _stamina.TakeStaminaDamage(uid, ling.MusclesStaminaDamage, null, null, null, false);
        }
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        //RemComp<ActivatableUIComponent>(uid);     // TODO: Исправить проблему с волосами слаймов
        //RemComp<UserInterfaceComponent>(uid);
        //RemComp<SlimeHairComponent>(uid);
        StealDNA(uid, component);

        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);
        _store.TryAddStore(uid,
                            new HashSet<ProtoId<CurrencyPrototype>> { "EvolutionPoints" },
                            new HashSet<ProtoId<StoreCategoryPrototype>> { "ChangelingAbilities" },
                            new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> { { "EvolutionPoints", 10 } },
                            false, false);
    }

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ChangelingEvolutionMenuActionEntity, component.ChangelingEvolutionMenuAction);
        _action.AddAction(uid, ref component.ChangelingRegenActionEntity, component.ChangelingRegenAction);
        _action.AddAction(uid, ref component.ChangelingAbsorbActionEntity, component.ChangelingAbsorbAction);
        _action.AddAction(uid, ref component.ChangelingDNAStingActionEntity, component.ChangelingDNAStingAction);
        _action.AddAction(uid, ref component.ChangelingDNACycleActionEntity, component.ChangelingDNACycleAction);
        _action.AddAction(uid, ref component.ChangelingStasisDeathActionEntity, component.ChangelingStasisDeathAction);
    }

    private void OnShutdown(EntityUid uid, ChangelingComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ChangelingEvolutionMenuActionEntity);
        _action.RemoveAction(uid, component.ChangelingRegenActionEntity);
        _action.RemoveAction(uid, component.ChangelingAbsorbActionEntity);
        _action.RemoveAction(uid, component.ChangelingDNAStingActionEntity);
        _action.RemoveAction(uid, component.ChangelingDNACycleActionEntity);
        _action.RemoveAction(uid, component.ChangelingStasisDeathActionEntity);
    }

    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingEvolutionMenuActionEvent args)
    {
        _store.OnInternalShop(uid);
    }

    public void OnCycleDNA(EntityUid uid, ChangelingComponent component, ChangelingCycleDNAActionEvent args) ///radial-menu
    {
        if (args.Handled)
            return;

        if (TryComp<ActorComponent>(uid, out var actorComponent))
        {
            var ev = new RequestChangelingFormsMenuEvent(GetNetEntity(uid), ChangelingMenuType.Transform);

            foreach (var item in component.StoredDNA)
            {
                var netEntity = GetNetEntity(item.EntityUid);

                ev.HumanoidData.Add(new(
                    netEntity,
                    Name(item.EntityUid),
                    item.HumanoidAppearanceComponent.Species.Id,
                    BuildProfile(item)));
            }

            // реализовать сортировку
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }
        args.Handled = true;
    }

    public void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        var selectedHumanoidData = component.StoredDNA[component.SelectedDNA];
        if (args.Handled)
            return;

        var dnaComp = EnsureComp<DnaComponent>(uid);

        if (selectedHumanoidData.DNA == dnaComp.DNA)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-already", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
            _popup.PopupEntity(selfMessage, uid, uid);
        }

        else if (component.ArmBladeActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-mutation");
            _popup.PopupEntity(selfMessage, uid, uid);
        }
        else if (component.LingArmorActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-mutation");
            _popup.PopupEntity(selfMessage, uid, uid);
        }
        else if (component.ChameleonSkinActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-mutation");
            _popup.PopupEntity(selfMessage, uid, uid);
        }
        else if (component.MusclesActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-mutation");
            _popup.PopupEntity(selfMessage, uid, uid);
        }
        else if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
        }

        else
        {
            if (!TryUseAbility(uid, component, component.ChemicalsCostFive))
                return;

            args.Handled = true;

            var transformedUid = _polymorph.PolymorphEntityAsHumanoid(uid, selectedHumanoidData);
            if (transformedUid == null)
                return;

            var selfMessage = Loc.GetString("changeling-transform-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
            _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);

            CopyLing(uid, transformedUid.Value);
        }
    }

    private void OnMobState(EntityUid uid, ChangelingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemoveBladeEntity(uid, component);
            RemoveShieldEntity(uid, component);
            RemCompDeferred<StealthComponent>(uid);
            RemCompDeferred<StealthOnMoveComponent>(uid);
            component.ChameleonSkinActive = false;
            return;
        }

        if (args.NewMobState != MobState.Dead && component.StasisDeathActive)
        {
            component.StasisDeathActive = false;
            _action.SetToggled(component.ChangelingStasisDeathActionEntity, component.StasisDeathActive);
        }
    }

    private void OnActionBought(EntityUid uid, ChangelingComponent component, ref ActionBoughtEvent args)
    {
        component.BoughtActions.Add(args.ActionEntity);
    }

    private void OnRefresh(EntityUid uid, ChangelingComponent component, ChangelingRefreshEvent args)
    {
        if (!component.CanRefresh)
        {
            _popup.PopupEntity(Loc.GetString("changeling-cant-refresh"), uid, uid);
            return;
        }

        foreach (var item in component.BoughtActions)
        {
            _action.RemoveAction(item);
        }

        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        RemoveBladeEntity(uid, component);
        RemoveShieldEntity(uid, component);
        RemCompDeferred<StealthComponent>(uid);
        RemCompDeferred<StealthOnMoveComponent>(uid);
        component.ChameleonSkinActive = false;

        if (component.LingArmorActive)
        {
            _inventorySystem.TryUnequip(uid, "head", true, true, false);
            _inventorySystem.TryUnequip(uid, "outerClothing", true, true, false);
            TryUseAbility(uid, component, 0f, false, component.LingArmorRegenCost);
            component.LingArmorActive = false;
        }

        _store.CloseUi(uid);
        RemComp<StoreComponent>(uid);   // Да, я не нашёл ничего лучше, чем сделать всё так
        _store.TryAddStore(uid,
                            new HashSet<ProtoId<CurrencyPrototype>> { "EvolutionPoints" },
                            new HashSet<ProtoId<StoreCategoryPrototype>> { "ChangelingAbilities" },
                            new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> { { "EvolutionPoints", 10 } },
                            false, false);

        component.CanRefresh = false;
    }

    private void OnSelectChangelingForm(SelectChangelingFormEvent ev)
    {
        var uid = GetEntity(ev.Target);
        if (ev.Type == ChangelingMenuType.Sting)
        {
            var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(GetEntity(ev.EntitySelected));
            if (newHumanoidData == null)
                return;

            _polymorph.PolymorphEntityAsHumanoid(GetEntity(ev.Target), newHumanoidData.Value);
            return;
        }

        if (!TryComp<ChangelingComponent>(uid, out var comp))
            return;

        TransformChangeling(uid, comp, ev);
    }

    public void CopyLing(EntityUid from, EntityUid to, ChangelingComponent? comp = null)
    {
        if (!Resolve(from, ref comp))
            return;
        if (HasComp<ChangelingComponent>(to))
            RemComp<ChangelingComponent>(to);

        var newLingComponent = EnsureComp<ChangelingComponent>(to);
        newLingComponent.Chemicals = comp.Chemicals;
        newLingComponent.ChemicalsPerSecond = comp.ChemicalsPerSecond;
        newLingComponent.StoredDNA = comp.StoredDNA;
        newLingComponent.SelectedDNA = comp.SelectedDNA;
        newLingComponent.ArmBladeActive = comp.ArmBladeActive;
        newLingComponent.ChameleonSkinActive = comp.ChameleonSkinActive;
        newLingComponent.LingArmorActive = comp.LingArmorActive;
        newLingComponent.CanRefresh = comp.CanRefresh;
        newLingComponent.LesserFormActive = comp.LesserFormActive;
        newLingComponent.AbsorbedDnaModifier = comp.AbsorbedDnaModifier;
        newLingComponent.SiliconStealthEnabled = comp.SiliconStealthEnabled;
        newLingComponent.BoughtActions = comp.BoughtActions;
        RemComp(from, comp);

        if (TryComp(from, out StoreComponent? storeComp))
        {
            var copiedStoreComponent = (Component)_serialization.CreateCopy(storeComp, notNullableOverride: true);
            RemComp<StoreComponent>(to);
            EntityManager.AddComponent(to, copiedStoreComponent);
        }

        if (TryComp(from, out StealthComponent? stealthComp)) // copy over stealth status
        {
            if (TryComp(from, out StealthOnMoveComponent? stealthOnMoveComp))
            {
                var copiedStealthComponent = (Component)_serialization.CreateCopy(stealthComp, notNullableOverride: true);
                EntityManager.AddComponent(to, copiedStealthComponent);
                RemComp(from, stealthComp);

                var copiedStealthOnMoveComponent = (Component)_serialization.CreateCopy(stealthOnMoveComp, notNullableOverride: true);
                EntityManager.AddComponent(to, copiedStealthOnMoveComponent);
                RemComp(from, stealthOnMoveComp);
            }
        }


        _actionContainer.TransferAllActionsWithNewAttached(from, to, to);
    }

    private HumanoidCharacterProfile BuildProfile(PolymorphHumanoidData data)
    {
        HumanoidCharacterAppearance hca = new();
        var humanoid = data.HumanoidAppearanceComponent;

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

        return new HumanoidCharacterProfile().
                WithCharacterAppearance(hca).
                WithSpecies(data.HumanoidAppearanceComponent.Species).
                WithSex(data.HumanoidAppearanceComponent.Sex).
                WithName(data.MetaDataComponent.EntityName);
    }

    public void TransformChangeling(EntityUid uid, ChangelingComponent component, SelectChangelingFormEvent ev)
    {
        int i = 0;
        var selectedEntity = GetEntity(ev.EntitySelected);
        foreach (var item in component.StoredDNA)
        {
            if (item.EntityUid == selectedEntity)
            {
                // transform
                var selectedHumanoidData = component.StoredDNA[i];
                if (ev.Handled)
                    return;

                var dnaComp = EnsureComp<DnaComponent>(uid);

                if (selectedHumanoidData.DNA == dnaComp.DNA)
                {
                    var selfMessage = Loc.GetString("changeling-transform-fail-already", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
                    _popup.PopupEntity(selfMessage, uid, uid);
                    return;
                }

                if (component.ArmBladeActive || component.LingArmorActive || component.ChameleonSkinActive || component.MusclesActive)
                {
                    var selfMessage = Loc.GetString("changeling-transform-fail-mutation");
                    _popup.PopupEntity(selfMessage, uid, uid);
                    return;
                }

                if (component.LesserFormActive && ev.Type != ChangelingMenuType.HumanForm)
                {
                    var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
                    _popup.PopupEntity(selfMessage, uid, uid);
                    return;
                }

                if (!TryUseAbility(uid, component, component.ChemicalsCostFive))
                    return;

                ev.Handled = true;

                var transformedUid = _polymorph.PolymorphEntityAsHumanoid(uid, selectedHumanoidData);
                if (transformedUid == null)
                    return;

                var message = Loc.GetString("changeling-transform-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
                _popup.PopupEntity(message, transformedUid.Value, transformedUid.Value);

                CopyLing(uid, transformedUid.Value);
            }
            i++;
        }
    }

    public void StealDNA(EntityUid uid, ChangelingComponent component)
    {
        var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(uid, uid);
        if (newHumanoidData == null)
            return;

        if (component.StoredDNA.Count >= component.DNAStrandCap)
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-full");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        else
        {
            component.StoredDNA.Add(newHumanoidData.Value);
        }

        return;
    }

    private bool TryStingTarget(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessageFailNoDna = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoDna, uid, uid);
            return false;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        if (!HasComp<DnaComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        return true;
    }

    public bool ChangeChemicalsAmount(EntityUid uid, float amount, ChangelingComponent? component = null, bool regenCap = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Chemicals += amount;

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        _alerts.ShowAlert(uid, _proto.Index<AlertPrototype>("Chemicals"), (short)Math.Clamp(Math.Round(component.Chemicals / 10.7f), 0, 7));

        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, float abilityCost, bool activated = true, float regenCost = 0f)
    {
        if (component.Chemicals <= Math.Abs(abilityCost) && activated)
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
            return false;
        }

        if (activated)
        {
            ChangeChemicalsAmount(uid, abilityCost, component, false);
            component.ChemicalsPerSecond -= regenCost;
        }
        else
        {
            component.ChemicalsPerSecond += regenCost;
        }

        return true;
    }

    public bool StealDNA(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(target);
        if (newHumanoidData == null)
            return false;

        else if (component.StoredDNA.Count >= component.DNAStrandCap)
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-full");
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        else
        {
            component.StoredDNA.Add(newHumanoidData.Value);
        }

        return true;
    }
}
