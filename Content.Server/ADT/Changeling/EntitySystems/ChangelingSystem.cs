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
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Content.Shared.Sirena.CollectiveMind;
using Content.Shared.Effects;
using System.Linq;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Preferences;
using Content.Server.Database;
using Content.Server.Humanoid;
using FastAccessors;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Markings;


namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
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
        RemComp<ThirstComponent>(uid); // changelings dont get hungry or thirsty
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

        return true;
    }

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ChangelingEvolutionMenuActionEntity, component.ChangelingEvolutionMenuAction);
        _action.AddAction(uid, ref component.ChangelingRegenActionEntity, component.ChangelingRegenAction);
        _action.AddAction(uid, ref component.ChangelingAbsorbActionEntity, component.ChangelingAbsorbAction);
        _action.AddAction(uid, ref component.ChangelingDNAStingActionEntity, component.ChangelingDNAStingAction);
        _action.AddAction(uid, ref component.ChangelingDNACycleActionEntity, component.ChangelingDNACycleAction);

        EnsureComp<CollectiveMindComponent>(uid);
        var collectiveMind = EnsureComp<CollectiveMindComponent>(uid);
        collectiveMind.Channel = component.HiveMind;
        collectiveMind.ShowRank = component.ShowRank;
        collectiveMind.ShowName = component.ShowName;
        collectiveMind.RankName = component.RankName;
    }

    private void OnShutdown(EntityUid uid, ChangelingComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ChangelingEvolutionMenuActionEntity);
        _action.RemoveAction(uid, component.ChangelingRegenActionEntity);
        _action.RemoveAction(uid, component.ChangelingAbsorbActionEntity);
        _action.RemoveAction(uid, component.ChangelingDNAStingActionEntity);
        _action.RemoveAction(uid, component.ChangelingDNACycleActionEntity);
        _action.RemoveAction(uid, component.ChangelingStasisDeathActionEntity);

        RemComp<CollectiveMindComponent>(uid);
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

            if (_mobState.IsDead(ling.Owner)) // if ling is dead dont regenerate chemicals
                return;

            if (ling.Chemicals < ling.MaxChemicals)
            {
                ChangeChemicalsAmount(uid, ling.ChemicalsPerSecond, ling, regenCap: true);
            }

            if (ling.MusclesActive)
            {
                _stamina.TakeStaminaDamage(uid, ling.MusclesStaminaDamage, null, null, null, false);
            }
            if (ling.ShieldEntity != null)
            {
                if (!TryComp<DamageableComponent>(ling.ShieldEntity.Value, out var damage))
                    return;

                var additionalShieldHealth = 50 * ling.AbsorbedDnaModifier;
                var shieldHealth = 150 + additionalShieldHealth;
                if (damage.TotalDamage >= shieldHealth)
                {
                    ling.ArmShieldActive = false;
                    QueueDel(ling.ShieldEntity.Value);
                    ling.ShieldEntity = new EntityUid?();

                    _audioSystem.PlayPvs(ling.SoundFlesh, uid);

                    var othersMessage = Loc.GetString("changeling-armshield-broke-others", ("user", Identity.Entity(uid, EntityManager)));
                    _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                    var selfMessage = Loc.GetString("changeling-armshield-broke-self");
                    _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
                }
            }
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
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

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
                if (netEntity == null)
                    continue;
                if (item.EntityUid == null)
                    continue;
                HumanoidCharacterAppearance hca = new();
                if (item.HumanoidAppearanceComponent == null)
                    continue;

                if (item.HumanoidAppearanceComponent.MarkingSet.Markings.TryGetValue(Shared.Humanoid.Markings.MarkingCategories.FacialHair, out var facialHair))
                    if (facialHair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithFacialHairStyleName(marking.MarkingId);
                        hca = hca.WithFacialHairColor(marking.MarkingColors.First());
                    }
                if (item.HumanoidAppearanceComponent.MarkingSet.Markings.TryGetValue(Shared.Humanoid.Markings.MarkingCategories.Hair, out var hair))
                    if (hair.TryGetValue(0, out var marking))
                    {
                        hca = hca.WithHairStyleName(marking.MarkingId);
                        hca = hca.WithHairColor(marking.MarkingColors.First());
                    }

                hca = hca.WithSkinColor(item.HumanoidAppearanceComponent.SkinColor);

                ev.HumanoidData.Add(new()
                {
                    NetEntity = netEntity.Value,
                    Name = Name(item.EntityUid.Value),
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
    public bool BlindSting(EntityUid uid, EntityUid target, ChangelingComponent component)  /// Ослепление
    {
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        if (!TryComp<StatusEffectsComponent>(target, out var statusComp))
            return false;

        _status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, TemporaryBlindnessSystem.BlindingStatusEffect, component.BlindStingDuration, true, statusComp);
        return true;
    }

    public bool MuteSting(EntityUid uid, EntityUid target, ChangelingComponent component)  /// чел ты в муте
    {
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        if (!_entityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
            return false;

        var muteInjection = new Solution(component.ChemicalMute, component.MuteAmount);
        _bloodstreamSystem.TryAddToChemicals(target, muteInjection, bloodstream);

        return true;
    }

    public bool DrugSting(EntityUid uid, EntityUid target, ChangelingComponent component)  /// чел ты в муте
    {
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        if (!_entityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
            return false;

        var drugInjection = new Solution(component.ChemicalSpaceDrugs, component.SpaceDrugsAmount);
        _bloodstreamSystem.TryAddToChemicals(target, drugInjection, bloodstream);

        return true;
    }


    public bool Adrenaline(EntityUid uid, ChangelingComponent component)    /// Адреналин
    {
        if (!TryComp<MetaDataComponent>(uid, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidappearance))
        {
            return false;
        }

        if (_tagSystem.HasTag(uid, "ChangelingBlacklist"))
        {
            return false;
        }
        if (!_entityManager.TryGetComponent<BloodstreamComponent>(uid, out var bloodstream))
            return false;
        var adrenalineInjection = new Solution(component.ChemicalMorphine, component.AdrenalineAmount);
        _bloodstreamSystem.TryAddToChemicals(uid, adrenalineInjection, bloodstream);

        var adrenalineInjectionTr = new Solution(component.ChemicalTranex, component.AdrenalineAmount);
        _bloodstreamSystem.TryAddToChemicals(uid, adrenalineInjectionTr, bloodstream);

        return true;
    }

    public bool OmniHeal(EntityUid uid, ChangelingComponent component)    /// Омнизин
    {
        if (!TryComp<MetaDataComponent>(uid, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidappearance))
        {
            return false;
        }

        if (_tagSystem.HasTag(uid, "ChangelingBlacklist"))
        {
            return false;
        }
        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(uid, damage_brute);
        _damageableSystem.TryChangeDamage(uid, damage_burn);
        _bloodstreamSystem.TryModifyBloodLevel(uid, component.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
        _bloodstreamSystem.TryModifyBleedAmount(uid, component.RegenerateBleedReduceAmount);
        _audioSystem.PlayPvs(component.SoundFleshQuiet, uid);
        /// Думали тут будет омнизин??? ХРЕН ВАМ!
        /// :3
        return true;
    }

    public bool Refresh(EntityUid uid, ChangelingComponent component)   /// Очистка
    {
        if (!component.CanRefresh)
        {
            var selfMessage = Loc.GetString("changeling-refresh-not-ready");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            return false;
        }

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return false;
        }

        else
        {
            component.StoredDNA = new List<PolymorphHumanoidData>();    /// Создание нового ДНК списка
            StealDNA(uid, component);   /// Сохранение ДНК текущего тела


            _store.TryAddCurrency(new Dictionary<string, FixedPoint2>   /// Костыльно перевёл сюда получение очков
            { {EvolutionPointsCurrencyPrototype, component.AbsorbedMobPointsAmount} }, uid);

            /// Удаление всех способностей - нихуя не понял, потом сделаю. Надеюсь.

            component.CanRefresh = false;
            _alertsSystem.ClearAlert(uid, _proto.Index<AlertPrototype>("ADTAlertLingRefresh"));

            component.SelectedDNA = 0;
            var selfMessage = Loc.GetString("changeling-refresh-self-success");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            return true;
        }

    }

    public bool Muscles(EntityUid uid, ChangelingComponent component)
    {
        if (!TryComp<MetaDataComponent>(uid, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidappearance))
        {
            return false;
        }

        if (!component.MusclesActive)
        {
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = movementSpeed.BaseSprintSpeed + component.MusclesModifier;
            var walkSpeed = movementSpeed.BaseWalkSpeed + component.MusclesModifier;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);
        }

        if (component.MusclesActive)
        {
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = movementSpeed.BaseSprintSpeed - component.MusclesModifier;
            var walkSpeed = movementSpeed.BaseWalkSpeed - component.MusclesModifier;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);
        }
        component.MusclesActive = !component.MusclesActive;
        return true;
    }

    public bool LesserForm(EntityUid uid, ChangelingComponent component)
    {

        if (component.ArmBladeActive)
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

        else
        {
            if (!component.LesserFormActive)
            {
                var transformedUid = _polymorph.PolymorphEntity(uid, component.LesserFormMob);
                if (transformedUid == null)
                    return false;

                var selfMessage = Loc.GetString("changeling-lesser-form-activate-monkey");
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
                newLingComponent.LesserFormActive = !component.LesserFormActive;
                newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;
                RemComp(uid, component);

                if (TryComp(uid, out StoreComponent? storeComp))
                {
                    var copiedStoreComponent = (Component) _serialization.CreateCopy(storeComp, notNullableOverride: true);
                    RemComp<StoreComponent>(transformedUid.Value);
                    EntityManager.AddComponent(transformedUid.Value, copiedStoreComponent);
                }

                if (TryComp(uid, out StealthComponent? stealthComp)) // copy over stealth status
                {
                    if (TryComp(uid, out StealthOnMoveComponent? stealthOnMoveComp))
                    {
                        var copiedStealthComponent = (Component) _serialization.CreateCopy(stealthComp, notNullableOverride: true);
                        EntityManager.AddComponent(transformedUid.Value, copiedStealthComponent);
                        RemComp(uid, stealthComp);

                        var copiedStealthOnMoveComponent = (Component) _serialization.CreateCopy(stealthOnMoveComp, notNullableOverride: true);
                        EntityManager.AddComponent(transformedUid.Value, copiedStealthOnMoveComponent);
                        RemComp(uid, stealthOnMoveComp);
                    }
                }

                _actionContainer.TransferAllActionsWithNewAttached(uid, transformedUid.Value, transformedUid.Value);

                if (!TryComp(transformedUid.Value, out InventoryComponent? inventory))
                    return false;
            }
            if (component.LesserFormActive)
            {
                var selectedHumanoidData = component.StoredDNA[component.SelectedDNA];

                var transformedUid = _polymorph.PolymorphEntityAsHumanoid(uid, selectedHumanoidData);
                if (transformedUid == null)
                    return false;

                if (selectedHumanoidData.EntityPrototype == null)
                    return false;
                if (selectedHumanoidData.HumanoidAppearanceComponent == null)
                    return false;
                if (selectedHumanoidData.MetaDataComponent == null)
                    return false;
                if (selectedHumanoidData.DNA == null)
                    return false;

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
                newLingComponent.LesserFormActive = !component.LesserFormActive;
                newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;
                RemComp(uid, component);

                if (TryComp(uid, out StoreComponent? storeComp))
                {
                    var copiedStoreComponent = (Component) _serialization.CreateCopy(storeComp, notNullableOverride: true);
                    RemComp<StoreComponent>(transformedUid.Value);
                    EntityManager.AddComponent(transformedUid.Value, copiedStoreComponent);
                }

                if (TryComp(uid, out StealthComponent? stealthComp)) // copy over stealth status
                {
                    if (TryComp(uid, out StealthOnMoveComponent? stealthOnMoveComp))
                    {
                        var copiedStealthComponent = (Component) _serialization.CreateCopy(stealthComp, notNullableOverride: true);
                        EntityManager.AddComponent(transformedUid.Value, copiedStealthComponent);
                        RemComp(uid, stealthComp);

                        var copiedStealthOnMoveComponent = (Component) _serialization.CreateCopy(stealthOnMoveComp, notNullableOverride: true);
                        EntityManager.AddComponent(transformedUid.Value, copiedStealthOnMoveComponent);
                        RemComp(uid, stealthOnMoveComp);
                    }
                }

                _actionContainer.TransferAllActionsWithNewAttached(uid, transformedUid.Value, transformedUid.Value);

                if (!TryComp(transformedUid.Value, out InventoryComponent? inventory))
                    return false;
            }

        }
        return true;
    }

    public const string LingSlugId = "ChangelingHeadslug";

    public bool SpawnLingSlug(EntityUid uid, ChangelingComponent component)
    {
        var slug = Spawn(LingSlugId, Transform(uid).Coordinates);

        var slugComp = EnsureComp<LingSlugComponent>(slug);
        slugComp.AbsorbedDnaModifier = component.AbsorbedDnaModifier;

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, slug, mind: mind);
        return true;
    }

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

    public bool ResonantShriek(EntityUid uid, ChangelingComponent component)
    {
        var xform = Transform(uid);
        foreach (var ent in _lookup.GetEntitiesInRange<StatusEffectsComponent>(xform.MapPosition, 15))
        {
            var time = TimeSpan.FromSeconds(6);

            if (TryComp<ChangelingComponent>(ent, out var ling))
                continue;

            _stun.TrySlowdown(ent, time, true, 0.8f, 0.8f);

            if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
                continue;
            if (mind.Session == null)
                continue;
            _audioSystem.PlayGlobal(component.SoundResonant, mind.Session);

            if (!TryComp<StatusEffectsComponent>(ent, out var statusComp))
                continue;

            _status.TryAddStatusEffect<TemporaryBlindnessComponent>(ent, TemporaryBlindnessSystem.BlindingStatusEffect, time, true, statusComp);
        }
        return true;
    }
}
