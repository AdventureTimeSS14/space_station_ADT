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
    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChangelingComponent, ChangelingEvolutionMenuActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, ChangelingCycleDNAActionEvent>(OnCycleDNA);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransform);

        SubscribeNetworkEvent<SelectChangelingFormEvent>(OnSelectChangelingForm);

        InitializeLingAbilities();
    }

    private void OnSelectChangelingForm(SelectChangelingFormEvent ev)
    {
        var uid = GetEntity(ev.Target);

        if (!TryComp<ChangelingComponent>(uid, out var comp))
            return;

        TransformChangeling(uid, comp, ev);
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
                            new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> { {"EvolutionPoints", 10 } },
                            false, false);
    }

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string EvolutionPointsCurrencyPrototype = "EvolutionPoints";

    [ValidatePrototypeId<StorePresetPrototype>]
    public const string ChangelingShopPresetPrototype = "StorePresetChangeling";

    public bool ChangeChemicalsAmount(EntityUid uid, float amount, ChangelingComponent? component = null, bool regenCap = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Chemicals += amount;

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        _alerts.ShowAlert(uid, _proto.Index<AlertPrototype>("Chemicals"), (short) Math.Clamp(Math.Round(component.Chemicals / 10.7f), 0, 7));

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

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ChangelingEvolutionMenuActionEntity, component.ChangelingEvolutionMenuAction);
        _action.AddAction(uid, ref component.ChangelingRegenActionEntity, component.ChangelingRegenAction);
        _action.AddAction(uid, ref component.ChangelingAbsorbActionEntity, component.ChangelingAbsorbAction);
        _action.AddAction(uid, ref component.ChangelingDNAStingActionEntity, component.ChangelingDNAStingAction);
        _action.AddAction(uid, ref component.ChangelingDNACycleActionEntity, component.ChangelingDNACycleAction);
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

    public void OnCycleDNA(EntityUid uid, ChangelingComponent component, ChangelingCycleDNAActionEvent args) ///radial-menu
    {
        if (args.Handled)
            return;


        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestChangelingFormsMenuEvent(GetNetEntity(uid));

            foreach (var item in component.StoredDNA)
            {
                var netEntity = GetNetEntity(item.EntityUid);
                HumanoidCharacterAppearance hca = new();
                if (item.HumanoidAppearanceComponent == null)
                    continue;

                if (item.HumanoidAppearanceComponent.MarkingSet.Markings.TryGetValue(MarkingCategories.FacialHair, out var facialHair))
                    if (facialHair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithFacialHairStyleName(marking.MarkingId);
                        hca = hca.WithFacialHairColor(marking.MarkingColors.First());
                    }
                if (item.HumanoidAppearanceComponent.MarkingSet.Markings.TryGetValue(MarkingCategories.Hair, out var hair))
                    if (hair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithHairStyleName(marking.MarkingId);
                        hca = hca.WithHairColor(marking.MarkingColors.First());
                    }

                hca = hca.WithSkinColor(item.HumanoidAppearanceComponent.SkinColor);

                ev.HumanoidData.Add(new()
                {
                    NetEntity = netEntity,
                    Name = Name(item.EntityUid),
                    Species = item.HumanoidAppearanceComponent.Species.Id,
                    Profile = new HumanoidCharacterProfile().WithCharacterAppearance(hca).WithSpecies(item.HumanoidAppearanceComponent.Species.Id)
                });
            }

            // реализовать сортировку
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);
        }
        args.Handled = true;
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

                if (selectedHumanoidData.EntityPrototype == null)
                {
                    var selfFailMessage = Loc.GetString("changeling-nodna-saved");
                    _popup.PopupEntity(selfFailMessage, uid, uid);
                    return;
                }
                if (selectedHumanoidData.HumanoidAppearanceComponent == null)
                {
                    var selfFailMessage = Loc.GetString("changeling-nodna-saved");
                    _popup.PopupEntity(selfFailMessage, uid, uid);
                    return;
                }
                if (selectedHumanoidData.MetaDataComponent == null)
                {
                    var selfFailMessage = Loc.GetString("changeling-nodna-saved");
                    _popup.PopupEntity(selfFailMessage, uid, uid);
                    return;
                }
                if (selectedHumanoidData.DNA == null)
                {
                    var selfFailMessage = Loc.GetString("changeling-nodna-saved");
                    _popup.PopupEntity(selfFailMessage, uid, uid);
                    return;
                }

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

                    ev.Handled = true;

                    var transformedUid = _polymorph.PolymorphEntityAsHumanoid(uid, selectedHumanoidData);
                    if (transformedUid == null)
                        return;

                    var selfMessage = Loc.GetString("changeling-transform-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
                    _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);

                    var newLingComponent = EnsureComp<ChangelingComponent>(transformedUid.Value);
                    newLingComponent.Chemicals = component.Chemicals;
                    newLingComponent.ChemicalsPerSecond = component.ChemicalsPerSecond;
                    newLingComponent.StoredDNA = component.StoredDNA;
                    newLingComponent.SelectedDNA = component.SelectedDNA;
                    newLingComponent.ArmBladeActive = component.ArmBladeActive;
                    newLingComponent.ChameleonSkinActive = component.ChameleonSkinActive;
                    newLingComponent.LingArmorActive = component.LingArmorActive;
                    newLingComponent.CanRefresh = component.CanRefresh;
                    newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;
                    RemComp(uid, component);

                    if (TryComp(uid, out StoreComponent? storeComp))
                    {
                        var copiedStoreComponent = (Component) _serialization.CreateCopy(storeComp, notNullableOverride: true);
                        RemComp<StoreComponent>(transformedUid.Value);
                        EntityManager.AddComponent(transformedUid.Value, copiedStoreComponent);
                    }

                    _actionContainer.TransferAllActionsWithNewAttached(uid, transformedUid.Value, transformedUid.Value);

                    if (!TryComp(transformedUid.Value, out InventoryComponent? inventory))
                        return;
                }
            }
            i++;
        }
    }
    public void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        var selectedHumanoidData = component.StoredDNA[component.SelectedDNA];
        if (args.Handled)
            return;

        var dnaComp = EnsureComp<DnaComponent>(uid);

        if (selectedHumanoidData.EntityPrototype == null)
        {
            var selfFailMessage = Loc.GetString("changeling-nodna-saved");
            _popup.PopupEntity(selfFailMessage, uid, uid);
            return;
        }
        if (selectedHumanoidData.HumanoidAppearanceComponent == null)
        {
            var selfFailMessage = Loc.GetString("changeling-nodna-saved");
            _popup.PopupEntity(selfFailMessage, uid, uid);
            return;
        }
        if (selectedHumanoidData.MetaDataComponent == null)
        {
            var selfFailMessage = Loc.GetString("changeling-nodna-saved");
            _popup.PopupEntity(selfFailMessage, uid, uid);
            return;
        }
        if (selectedHumanoidData.DNA == null)
        {
            var selfFailMessage = Loc.GetString("changeling-nodna-saved");
            _popup.PopupEntity(selfFailMessage, uid, uid);
            return;
        }

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

            var newLingComponent = EnsureComp<ChangelingComponent>(transformedUid.Value);
            newLingComponent.Chemicals = component.Chemicals;
            newLingComponent.ChemicalsPerSecond = component.ChemicalsPerSecond;
            newLingComponent.StoredDNA = component.StoredDNA;
            newLingComponent.SelectedDNA = component.SelectedDNA;
            newLingComponent.ArmBladeActive = component.ArmBladeActive;
            newLingComponent.ChameleonSkinActive = component.ChameleonSkinActive;
            newLingComponent.LingArmorActive = component.LingArmorActive;
            newLingComponent.CanRefresh = component.CanRefresh;
            newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;
            RemComp(uid, component);

            if (TryComp(uid, out StoreComponent? storeComp))
            {
                var copiedStoreComponent = (Component) _serialization.CreateCopy(storeComp, notNullableOverride: true);
                RemComp<StoreComponent>(transformedUid.Value);
                EntityManager.AddComponent(transformedUid.Value, copiedStoreComponent);
            }

            _actionContainer.TransferAllActionsWithNewAttached(uid, transformedUid.Value, transformedUid.Value);

            if (!TryComp(transformedUid.Value, out InventoryComponent? inventory))
                return;
        }
    }

    public const string LingSlugId = "ChangelingHeadslug";

    // public bool SpawnLingSlug(EntityUid uid, ChangelingComponent component)
    // {
    //     var slug = Spawn(LingSlugId, Transform(uid).Coordinates);

    //     var slugComp = EnsureComp<LingSlugComponent>(slug);
    //     slugComp.AbsorbedDnaModifier = component.AbsorbedDnaModifier;

    //     if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
    //         _mindSystem.TransferTo(mindId, slug, mind: mind);
    //     return true;
    // }

    public const string LingMonkeyId = "MobMonkeyChangeling";

    public bool SpawnLingMonkey(EntityUid uid, ChangelingComponent component)
    {
        var slug = Spawn(LingMonkeyId, Transform(uid).Coordinates);

        var newLingComponent = EnsureComp<ChangelingComponent>(slug);
        newLingComponent.Chemicals = component.Chemicals;
        newLingComponent.ChemicalsPerSecond = component.ChemicalsPerSecond;
        newLingComponent.StoredDNA = component.StoredDNA;
        newLingComponent.SelectedDNA = component.SelectedDNA;
        newLingComponent.ArmBladeActive = component.ArmBladeActive;
        newLingComponent.ChameleonSkinActive = component.ChameleonSkinActive;
        newLingComponent.LingArmorActive = component.LingArmorActive;
        newLingComponent.CanRefresh = component.CanRefresh;
        newLingComponent.LesserFormActive = !component.LesserFormActive;
        newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;


        RemComp(uid, component);

        _action.AddAction(slug, ref component.ChangelingLesserFormActionEntity, component.ChangelingLesserFormAction);


        newLingComponent.StoredDNA = new List<PolymorphHumanoidData>();    /// Создание нового ДНК списка
        var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(uid);
        if (newHumanoidData == null)
            return false;

        else
        {
            newLingComponent.StoredDNA.Add(newHumanoidData.Value);
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, slug, mind: mind);
        if (mind != null)
            mind.PreventGhosting = false;
        return true;
    }

}
