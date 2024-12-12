using Content.Server.Actions;
using Content.Shared.Inventory;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Content.Shared.Store;
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
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Server.Stunnable;
using Content.Shared.Mind;
using Robust.Shared.Player;
using System.Linq;
using Content.Shared.Preferences;
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
using Content.Shared.Mobs;
using Content.Server.Stealth;
using Content.Server.ADT.Store;
using Robust.Server.Containers;
using Content.Shared.ADT.Stealth.Components;
using Content.Shared.Sirena.CollectiveMind;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    #region Dependency
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly FlashSystem _flashSystem = default!;
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
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
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
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransform);

        SubscribeNetworkEvent<SelectChangelingFormEvent>(OnSelectChangelingForm);

        InitializeUsefulAbilities();
        InitializeCombatAbilities();
        InitializeSlug();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var lingQuery = EntityQueryEnumerator<ChangelingComponent>();
        while (lingQuery.MoveNext(out var uid, out var comp))
        {
            UpdateChangeling(uid, frameTime, comp);
        }

        var slugQuery = EntityQueryEnumerator<ChangelingHeadslugComponent>();
        while (slugQuery.MoveNext(out var uid, out var comp))
        {
            UpdateChangelingHeadslug(uid, frameTime, comp);
        }

        var transformedQuery = EntityQueryEnumerator<ForceTransformedComponent>();
        while (transformedQuery.MoveNext(out var uid, out var comp))
        {
            UpdateTransformed(uid, frameTime, comp);
        }
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
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
        EnsureComp<CollectiveMindComponent>(uid);
        if (component.GainedActions)
            return;
        _action.AddAction(uid, ref component.ChangelingEvolutionMenuActionEntity, component.ChangelingEvolutionMenuAction);
        _action.AddAction(uid, ref component.ChangelingRegenActionEntity, component.ChangelingRegenAction);
        _action.AddAction(uid, ref component.ChangelingAbsorbActionEntity, component.ChangelingAbsorbAction);
        _action.AddAction(uid, ref component.ChangelingDNAStingActionEntity, component.ChangelingDNAStingAction);
        _action.AddAction(uid, ref component.ChangelingDNACycleActionEntity, component.ChangelingDNACycleAction);
        _action.AddAction(uid, ref component.ChangelingStasisDeathActionEntity, component.ChangelingStasisDeathAction);

        component.BasicTransferredActions.Add(component.ChangelingEvolutionMenuActionEntity);
        component.BasicTransferredActions.Add(component.ChangelingRegenActionEntity);
        component.BasicTransferredActions.Add(component.ChangelingAbsorbActionEntity);
        component.BasicTransferredActions.Add(component.ChangelingDNAStingActionEntity);
        component.BasicTransferredActions.Add(component.ChangelingDNACycleActionEntity);
        component.BasicTransferredActions.Add(component.ChangelingStasisDeathActionEntity);
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

    public void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        if (args.Handled)
            return;
        if (component.StoredDNA.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-nodna"), uid, uid);
            return;
        }

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

    private void OnMobState(EntityUid uid, ChangelingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemoveBladeEntity(uid, component);
            RemoveShieldEntity(uid, component);
            RemoveArmaceEntity(uid, component);
            RemCompDeferred<StealthComponent>(uid);
            RemCompDeferred<StealthOnMoveComponent>(uid);
            RemCompDeferred<DigitalCamouflageComponent>(uid);
            component.ChameleonSkinActive = false;
            component.DigitalCamouflageActive = false;
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
            QueueDel(item);
        }

        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        RemoveBladeEntity(uid, component);
        RemoveShieldEntity(uid, component);
        RemoveArmaceEntity(uid, component);
        RemCompDeferred<StealthComponent>(uid);
        RemCompDeferred<StealthOnMoveComponent>(uid);
        RemCompDeferred<DigitalCamouflageComponent>(uid);
        component.ChameleonSkinActive = false;
        component.DigitalCamouflageActive = false;

        if (component.LingArmorActive)
        {
            _inventorySystem.TryUnequip(uid, "head", true, true, false);
            _inventorySystem.TryUnequip(uid, "outerClothing", true, true, false);
            TryUseAbility(uid, component, 0f, false, component.LingArmorRegenCost);
            component.LingArmorActive = false;
        }

        component.BoughtActions.Clear();

        _store.TrySetCurrency(new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> { { "EvolutionPoints", 10 + (5 * component.ChangelingsAbsorbed) } }, uid);
        _store.TryRefreshStoreStock(uid);
        component.CanRefresh = false;

        _store.UpdateUserInterface(uid, uid);
    }

    private void OnSelectChangelingForm(SelectChangelingFormEvent ev)
    {
        var uid = GetEntity(ev.User);
        var target = GetEntity(ev.Target);
        var selected = GetEntity(ev.EntitySelected);

        if (!TryComp<ChangelingComponent>(uid, out var comp))
            return;

        if (ev.Type == ChangelingMenuType.Sting)
        {
            var list = comp.StoredDNA.Where(x => x.EntityUid == selected);
            if (list.Count() <= 0)
                return;

            var newHumanoidData = _polymorph.CopyPolymorphHumanoidData(list.First());
            var data = _polymorph.TryRegisterPolymorphHumanoidData(target, target);
            var polymorphed = _polymorph.PolymorphEntityAsHumanoid(target, newHumanoidData);
            if (polymorphed.HasValue)
            {
                var forcedComp = EnsureComp<ForceTransformedComponent>(polymorphed.Value);
                forcedComp.OriginalBody = data;
                forcedComp.RevertAt = _timing.CurTime + TimeSpan.FromMinutes(15);
            }

            comp.StoredDNA.Remove(list.First());
            return;
        }

        TransformChangeling(uid, comp, ev);
    }

    public void CopyLing(EntityUid from, EntityUid to, ChangelingComponent? comp = null)
    {
        if (!Resolve(from, ref comp))
            return;
        if (HasComp<ChangelingComponent>(to))
            RemComp<ChangelingComponent>(to);

        var newLingComponent = new ChangelingComponent();
        newLingComponent.Chemicals = comp.Chemicals;
        newLingComponent.ChemicalsPerSecond = comp.ChemicalsPerSecond;

        newLingComponent.StoredDNA = comp.StoredDNA;

        newLingComponent.ArmBladeActive = comp.ArmBladeActive;
        newLingComponent.ChameleonSkinActive = comp.ChameleonSkinActive;
        newLingComponent.LingArmorActive = comp.LingArmorActive;
        newLingComponent.LesserFormActive = comp.LesserFormActive;

        newLingComponent.AbsorbedDnaModifier = comp.AbsorbedDnaModifier;
        newLingComponent.DNAStolen = comp.DNAStolen;
        newLingComponent.ChangelingsAbsorbed = comp.ChangelingsAbsorbed;
        newLingComponent.LastResortUsed = comp.LastResortUsed;
        newLingComponent.CanRefresh = comp.CanRefresh;

        newLingComponent.BasicTransferredActions = comp.BasicTransferredActions;
        newLingComponent.BoughtActions = comp.BoughtActions;
        newLingComponent.GainedActions = true;
        RemComp(from, comp);
        AddComp(to, newLingComponent);

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

        foreach (var basic in comp.BasicTransferredActions)
        {
            if (basic.HasValue)
                _actionContainer.TransferActionWithNewAttached(basic.Value, to, to);
        }
        foreach (var item in comp.BoughtActions)
        {
            if (item.HasValue)
                _actionContainer.TransferActionWithNewAttached(item.Value, to, to);
        }

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
        var selectedEntity = GetEntity(ev.EntitySelected);
        var list = component.StoredDNA.Where(x => x.EntityUid == selectedEntity);
        if (list.Count() <= 0)
            return;

        var selectedHumanoidData = list.First();

        var dnaComp = EnsureComp<DnaComponent>(uid);

        if (selectedHumanoidData.DNA == dnaComp.DNA)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-already", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (component.ArmBladeActive || component.LingArmorActive || component.ChameleonSkinActive || component.MusclesActive || component.DigitalCamouflageActive)
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

        if (!TryUseAbility(uid, component, 5f))
            return;

        foreach (var item in component.BoughtActions.Where(x =>
                                                            x.HasValue &&
                                                            TryPrototype(x.Value, out var proto) &&
                                                            proto.ID == "ActionLingLesserForm"))
        {
            _action.SetToggled(item, false);
        }

        component.LesserFormActive = false;

        component.StoredDNA.Remove(selectedHumanoidData);

        var transformedUid = _polymorph.PolymorphEntityAsHumanoid(uid, selectedHumanoidData);
        if (transformedUid == null)
            return;

        var message = Loc.GetString("changeling-transform-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
        _popup.PopupEntity(message, transformedUid.Value, transformedUid.Value);

        CopyLing(uid, transformedUid.Value);
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

    private bool TryStingTarget(EntityUid uid, EntityUid target)
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
            ChangeChemicalsAmount(uid, -abilityCost, component, false);
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
        else if (HasComp<ForceTransformedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-sting-fail-already", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return false;
        }

        else
        {
            component.DNAStolen++;
            component.StoredDNA.Add(newHumanoidData.Value);
        }

        return true;
    }

    private void UpdateChangeling(EntityUid uid, float frameTime, ChangelingComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Accumulator += frameTime;

        if (comp.Accumulator <= 1)
            return;
        comp.Accumulator -= 1;

        if (comp.Chemicals < comp.MaxChemicals)
            ChangeChemicalsAmount(uid, _mobState.IsDead(uid) ? comp.ChemicalsPerSecond / 2 : comp.ChemicalsPerSecond, comp, regenCap: true);

        if (comp.MusclesActive)
            _stamina.TakeStaminaDamage(uid, comp.MusclesStaminaDamage, null, null, null, false);

    }

    private void UpdateTransformed(EntityUid uid, float frameTime, ForceTransformedComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp.RevertAt > _timing.CurTime)
            return;

        if (!comp.OriginalBody.HasValue)
            return;

        _polymorph.PolymorphEntityAsHumanoid(uid, comp.OriginalBody.Value);
    }
}
