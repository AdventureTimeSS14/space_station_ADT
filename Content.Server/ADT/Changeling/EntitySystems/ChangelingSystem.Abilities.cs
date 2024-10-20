using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Server.Body.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Content.Shared.Stealth.Components;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Server.Forensics;
using Content.Shared.FixedPoint;
using Content.Server.Store.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs;
using Content.Server.Destructible;
using Content.Server.Ghost.Components;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Rejuvenate;
using Content.Server.Cuffs;
using Content.Shared.Polymorph;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
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

    private void InitializeLingAbilities()
    {
        //проверяет есть ли активные абилки в виде щита или руки-клинка, удаляет их после смерти.
        SubscribeLocalEvent<ChangelingComponent, MobStateChangedEvent>(OnMobState);

        SubscribeLocalEvent<ChangelingComponent, LingAbsorbActionEvent>(StartAbsorbing);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, LingRegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);
        SubscribeLocalEvent<ChangelingComponent, LingInvisibleActionEvent>(OnLingInvisible);
        SubscribeLocalEvent<ChangelingComponent, LingEMPActionEvent>(OnLingEmp);
        SubscribeLocalEvent<ChangelingComponent, LingStingExtractActionEvent>(OnLingDNASting);
        SubscribeLocalEvent<ChangelingComponent, StasisDeathActionEvent>(OnStasisDeathAction);
        SubscribeLocalEvent<ChangelingComponent, BlindStingEvent>(OnBlindSting);
        SubscribeLocalEvent<ChangelingComponent, AdrenalineActionEvent>(OnAdrenaline);
        SubscribeLocalEvent<ChangelingComponent, OmniHealActionEvent>(OnOmniHeal);
        SubscribeLocalEvent<ChangelingComponent, MuteStingEvent>(OnMuteSting);
        SubscribeLocalEvent<ChangelingComponent, DrugStingEvent>(OnDrugSting);
        SubscribeLocalEvent<ChangelingComponent, ChangelingMusclesActionEvent>(OnMuscles);
        SubscribeLocalEvent<ChangelingComponent, ChangelingLesserFormActionEvent>(OnLesserForm);
        SubscribeLocalEvent<ChangelingComponent, ArmShieldActionEvent>(OnArmShieldAction);
        SubscribeLocalEvent<ChangelingComponent, LastResortActionEvent>(OnLastResort);
        SubscribeLocalEvent<ChangelingComponent, LingBiodegradeActionEvent>(OnBiodegrade);
        SubscribeLocalEvent<ChangelingComponent, BiodegradeDoAfterEvent>(OnBiodegradeDoAfter);
        SubscribeLocalEvent<ChangelingComponent, LingResonantShriekEvent>(OnResonantShriek);
        SubscribeLocalEvent<ChangelingComponent, TransformationStingEvent>(OnTransformSting);
    }

    // Метод для удаления BladeEntity
    private void RemoveBladeEntity(EntityUid bladeUid, EntityUid uid, ChangelingComponent component)
    {
        QueueDel(bladeUid);
        _audioSystem.PlayPvs(component.SoundFlesh, uid);

        var othersMessage = Loc.GetString("changeling-armblade-retract-others", ("user", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

        var selfMessage = Loc.GetString("changeling-armblade-retract-self");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

        component.BladeEntity = new EntityUid?();
    }

    // Метод для удаления ShieldEntity
    private void RemoveShieldEntity(EntityUid shieldUid, EntityUid uid, ChangelingComponent component)
    {
        QueueDel(shieldUid);
        _audioSystem.PlayPvs(component.SoundFlesh, uid);

        var othersMessage = Loc.GetString("changeling-armshield-retract-others", ("user", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

        var selfMessage = Loc.GetString("changeling-armshield-retract-self");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

        component.ShieldEntity = new EntityUid?();
    }

    private void OnMobState(EntityUid uid, ChangelingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            // Проверяем наличие BladeEntity и удаляем его
            if (component.BladeEntity != null)
            {
                RemoveBladeEntity(component.BladeEntity.Value, uid, component);
            }
            // Снимаем флаг активации руки-клинка
            component.ArmBladeActive = false;

            // Проверяем наличие ShieldEntity и удаляем его
            if (component.ShieldEntity != null)
            {
                RemoveShieldEntity(component.ShieldEntity.Value, uid, component);
            }
            // Снимаем флаг активации щита
            component.ArmShieldActive = false;
        }
    }

    private void StartAbsorbing(EntityUid uid, ChangelingComponent component, LingAbsorbActionEvent args)   // Начало поглощения
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var target = args.Target;
        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_mobState.IsIncapacitated(target)) // if target isn't crit or dead dont let absorb
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-notdead", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-alreadyabsorbed", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }


        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("changeling-dna-stage-1"), uid, uid);

        var doAfter = new DoAfterArgs(EntityManager, uid, component.AbsorbDuration, new AbsorbDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";
    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDoAfterEvent args)     // DoAfter, та полоска над персонажем
    {
        if (args.Handled || args.Args.Target == null)
            return;

        args.Handled = true;
        args.Repeat = RepeatDoAfter(component);
        var target = args.Args.Target.Value;

        if (args.Cancelled || !_mobState.IsIncapacitated(target) || HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-interrupted", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            component.AbsorbStage = 0;
            args.Repeat = false;
            return;
        }

        if (component.AbsorbStage == 0)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-2-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-2-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else if (component.AbsorbStage == 1)
        {
            var othersMessage = Loc.GetString("changeling-dna-stage-3-others", ("user", Identity.Entity(uid, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.LargeCaution);

            var selfMessage = Loc.GetString("changeling-dna-stage-3-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.LargeCaution);
        }
        else if (component.AbsorbStage == 2)
        {
            var doStealDNA = true;
            if (TryComp(target, out DnaComponent? dnaCompTarget))
            {
                foreach (var storedData in component.StoredDNA)
                {
                    if (storedData.DNA != null && storedData.DNA == dnaCompTarget.DNA)
                        doStealDNA = false;
                }
            }

            if (doStealDNA)
            {
                if (!StealDNA(uid, target, component))
                {
                    component.AbsorbStage = 0;
                    args.Repeat = false;
                    return;
                }
            }

            // Нанесение 200 генетического урона и замена крови на кислоту
            var dmg = new DamageSpecifier(_proto.Index(GeneticDamageGroup), component.AbsorbGeneticDmg);
            _damageableSystem.TryChangeDamage(target, dmg);
            _bloodstreamSystem.ChangeBloodReagent(target, "FerrochromicAcid"); // Замена крови на кислоту
            _bloodstreamSystem.SpillAllSolutions(target); // Выплёскивание всей кислоты из тела
            EnsureComp<AbsorbedComponent>(target);

            if (HasComp<ChangelingComponent>(target)) // Если это был другой генокрад, получим моментально 5 очков эволюции
            {
                var selfMessage = Loc.GetString("changeling-dna-success-ling", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);

                if (TryComp<StoreComponent>(uid, out var store))
                {
                    _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { EvolutionPointsCurrencyPrototype, component.AbsorbedChangelingPointsAmount } }, uid, store);
                    _store.UpdateUserInterface(uid, uid, store);
                }
            }
            else  // Если это не был генокрад, получаем возможность "сброса"
            {
                var selfMessage = Loc.GetString("changeling-dna-success", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);
                component.CanRefresh = true;
                _alertsSystem.ShowAlert(uid, AlertType.ADTAlertLingRefresh);
                component.AbsorbedDnaModifier = component.AbsorbedDnaModifier + 1;
            }
        }

        if (component.AbsorbStage >= 2)
            component.AbsorbStage = 0;
        else
            component.AbsorbStage += 1;
    }

    private static bool RepeatDoAfter(ChangelingComponent component)    // Повторение DoAfter'а
    {
        if (component.AbsorbStage < 2.0)
            return true;
        else
            return false;
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    private void OnRegenerate(EntityUid uid, ChangelingComponent component, LingRegenerateActionEvent args)     // Реген в крите
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (_mobState.IsDead(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-dead"), uid, uid);
            return;
        }

        if (_mobState.IsCritical(uid)) // make sure the ling is critical, if not they cant regenerate
        {
            if (!TryUseAbility(uid, component, component.ChemicalsCostTen))
                return;

            args.Handled = true;

            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
            _damageableSystem.TryChangeDamage(uid, damage_brute);
            _damageableSystem.TryChangeDamage(uid, damage_burn);
            _bloodstreamSystem.TryModifyBloodLevel(uid, component.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
            _bloodstreamSystem.TryModifyBleedAmount(uid, component.RegenerateBleedReduceAmount);
            _audioSystem.PlayPvs(component.SoundRegenerate, uid);

            var othersMessage = Loc.GetString("changeling-regenerate-others-success", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-regenerate-self-success");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-not-crit"), uid, uid);
        }
    }

    public const string ArmBladeId = "ArmBlade";
    private void OnArmBladeAction(EntityUid uid, ChangelingComponent component, ArmBladeActionEvent args)   // При нажатии на действие армблейда
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmBladeActive))
            return;

        args.Handled = true;

        if (!component.ArmBladeActive)
        {
            if (SpawnArmBlade(uid, component))
            {
                component.ArmBladeActive = true;
                _audioSystem.PlayPvs(component.SoundFlesh, uid);

                var othersMessage = Loc.GetString("changeling-armblade-success-others", ("user", Identity.Entity(uid, EntityManager)));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("changeling-armblade-success-self");
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("changeling-armblade-fail"), uid, uid);
            }
        }
        else
        {
            if (component.BladeEntity != null)
            {
                RemoveBladeEntity(component.BladeEntity.Value, uid, component);
            }

            component.ArmBladeActive = false;
        }
    }

    public const string ArmShieldId = "ArmShield";
    private void OnArmShieldAction(EntityUid uid, ChangelingComponent component, ArmShieldActionEvent args)     // При нажатии на действие орг. щита
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmShieldActive))
            return;

        args.Handled = true;

        if (!component.ArmShieldActive)
        {
            if (SpawnArmShield(uid, component))
            {
                component.ArmShieldActive = true;
                _audioSystem.PlayPvs(component.SoundFlesh, uid);

                var othersMessage = Loc.GetString("changeling-armshield-success-others", ("user", Identity.Entity(uid, EntityManager)));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("changeling-armshield-success-self");
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("changeling-armshield-fail"), uid, uid);
            }
        }
        else
        {
            if (component.ShieldEntity != null)
            {
                RemoveShieldEntity(component.ShieldEntity.Value, uid, component);
            }

            component.ArmShieldActive = false;
        }
    }

    public void SpawnLingArmor(EntityUid uid, InventoryComponent inventory)     // Спавн хитиновой брони
    {
        var helmet = Spawn(LingHelmetId, Transform(uid).Coordinates);
        var armor = Spawn(LingArmorId, Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(helmet); // cant remove the armor
        EnsureComp<UnremoveableComponent>(armor); // cant remove the armor

        _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
        _inventorySystem.TryEquip(uid, helmet, HeadId, true, true, false, inventory);
        _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
        _inventorySystem.TryEquip(uid, armor, OuterClothingId, true, true, false, inventory);
    }

    public bool SpawnArmBlade(EntityUid uid, ChangelingComponent component)     // Спавн руки-клинка
    {
        var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..
        RemComp<DestructibleComponent>(armblade);
        if (_handsSystem.TryPickupAnyHand(uid, armblade))
        {
            if (!TryComp(uid, out HandsComponent? handsComponent))
                return false;
            if (handsComponent.ActiveHand == null)
                return false;
            component.BladeEntity = armblade;
            return true;
        }
        else
        {
            QueueDel(armblade);
            return false;
        }
    }

    public bool SpawnArmShield(EntityUid uid, ChangelingComponent component)    // Спавн щита
    {
        var armshield = Spawn(ArmShieldId, Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(armshield); // armblade is apart of your body.. cant remove it..


        if (_handsSystem.TryPickupAnyHand(uid, armshield))
        {
            if (!TryComp(uid, out HandsComponent? handsComponent))
                return false;
            if (handsComponent.ActiveHand == null)
                return false;
            component.ShieldEntity = armshield;
            return true;
        }
        else
        {
            QueueDel(armshield);
            return false;
        }
    }


    public const string LingHelmetId = "ClothingHeadHelmetLing";
    public const string LingArmorId = "ClothingOuterArmorChangeling";
    public const string HeadId = "head";
    public const string OuterClothingId = "outerClothing";

    private void OnLingArmorAction(EntityUid uid, ChangelingComponent component, LingArmorActionEvent args)     // При нажатии на действие хитиновой брони
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        _audioSystem.PlayPvs(component.SoundFlesh, uid);

        if (!component.LingArmorActive)
        {
            args.Handled = true;

            SpawnLingArmor(uid, inventory);

            var othersMessage = Loc.GetString("changeling-armor-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armor-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            if (_inventorySystem.TryGetSlotEntity(uid, HeadId, out var headitem) && _inventorySystem.TryGetSlotEntity(uid, OuterClothingId, out var outerclothingitem))
            {
                if (TryComp<MetaDataComponent>(headitem, out var targetHelmetMeta))
                {
                    if (TryPrototype(headitem.Value, out var prototype, targetHelmetMeta))
                    {
                        if (prototype.ID == LingHelmetId)
                        {
                            _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
                        }
                    }
                }

                if (TryComp<MetaDataComponent>(outerclothingitem, out var targetArmorMeta))
                {
                    if (TryPrototype(outerclothingitem.Value, out var prototype, targetArmorMeta))
                    {
                        if (prototype.ID == LingArmorId)
                        {
                            _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
                        }
                    }
                }

                var othersMessage = Loc.GetString("changeling-armor-retract-others", ("user", Identity.Entity(uid, EntityManager)));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("changeling-armor-retract-self");
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

                var solution = new Solution();
                solution.AddReagent("Blood", FixedPoint2.New(75));
                _puddle.TrySpillAt(Transform(uid).Coordinates, solution, out _);
            }
        }

        component.LingArmorActive = !component.LingArmorActive;
    }

    private void OnLingInvisible(EntityUid uid, ChangelingComponent component, LingInvisibleActionEvent args)    // При нажатии на действие невидимости
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive, !component.ChameleonSkinActive))
            return;

        args.Handled = true;

        var stealth = EnsureComp<StealthComponent>(uid); // cant remove the armor
        var stealthonmove = EnsureComp<StealthOnMoveComponent>(uid); // cant remove the armor

        var message = Loc.GetString(!component.ChameleonSkinActive ? "changeling-chameleon-toggle-on" : "changeling-chameleon-toggle-off");
        _popup.PopupEntity(message, uid, uid);

        if (!component.ChameleonSkinActive)
        {
            stealthonmove.PassiveVisibilityRate = component.ChameleonSkinPassiveVisibilityRate;
            stealthonmove.MovementVisibilityRate = component.ChameleonSkinMovementVisibilityRate;
        }
        else
        {
            RemCompDeferred(uid, stealth);
            RemCompDeferred(uid, stealthonmove);
        }

        component.ChameleonSkinActive = !component.ChameleonSkinActive;
    }

    private void OnLingEmp(EntityUid uid, ChangelingComponent component, LingEMPActionEvent args)       // При нажатии на ЭМИ действие
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(uid);
        _emp.EmpPulse(coords, component.DissonantShriekEmpRange, component.DissonantShriekEmpConsumption, component.DissonantShriekEmpDuration);
    }

    // changeling stings
    private void OnLingDNASting(EntityUid uid, ChangelingComponent component, LingStingExtractActionEvent args)     // Жало кражи днк
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessageFailNoDna = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoDna, uid, uid);
            return;
        }

        var dnaCompTarget = EnsureComp<DnaComponent>(target);

        foreach (var storedData in component.StoredDNA)
        {
            if (storedData.DNA != null && storedData.DNA == dnaCompTarget.DNA)
            {
                var selfMessageFailAlreadyDna = Loc.GetString("changeling-dna-sting-fail-alreadydna", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessageFailAlreadyDna, uid, uid);
                return;
            }
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (!HasComp<DnaComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (component.StoredDNA.Count >= component.DNAStrandCap)
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-full");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
            return;

        if (StealDNA(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-dna-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }
    }

    private void OnStasisDeathAction(EntityUid uid, ChangelingComponent component, StasisDeathActionEvent args)     // При нажатии на действие стазис-смерти
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!component.StasisDeathActive)
        {
            if (!_mobState.IsDead(uid))
            {
                if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
                    return;

                args.Handled = true;

                if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                    mind.PreventGhosting = true;

                var damage_burn = new DamageSpecifier(_proto.Index(GeneticDamageGroup), component.StasisDeathDamageAmount);
                _damageableSystem.TryChangeDamage(uid, damage_burn);    /// Самоопиздюливание

                component.StasisDeathActive = true;

                var selfMessage = Loc.GetString("changeling-stasis-death-self-success");  /// всё, я спать откисать, адьос
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            }
            else
            {
                args.Handled = true;

                if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                    mind.PreventGhosting = true;

                var damage_burn = new DamageSpecifier(_proto.Index(GeneticDamageGroup), component.StasisDeathDamageAmount);
                _damageableSystem.TryChangeDamage(uid, damage_burn);    /// Самоопиздюливание

                component.StasisDeathActive = true;

                var selfMessage = Loc.GetString("changeling-stasis-death-self-success");  /// всё, я спать откисать, адьос
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
            }
        }
        else
        {

            if (_mobState.IsDead(uid) && component.StasisDeathActive)
            {

                if (!TryUseAbility(uid, component, component.ChemicalsCostFree))
                    return;

                args.Handled = true;

                var selfMessage = Loc.GetString("changeling-stasis-death-self-revive");  /// вейк ап энд cum бэк ту ворк
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

                _audioSystem.PlayPvs(component.SoundRegenerate, uid);

                RaiseLocalEvent(uid, new RejuvenateEvent());

                component.StasisDeathActive = false;

                if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                    mind.PreventGhosting = false;
            }
        }

    }
    private void OnBlindSting(EntityUid uid, ChangelingComponent component, BlindStingEvent args)   // Жало ослепления
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!HasComp<DnaComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostFifteen))
            return;

        if (BlindSting(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }

    }

    private void OnMuteSting(EntityUid uid, ChangelingComponent component, MuteStingEvent args)     // Жало безмолвия
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!HasComp<DnaComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        if (MuteSting(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }

    }

    private void OnDrugSting(EntityUid uid, ChangelingComponent component, DrugStingEvent args)     // Галлюценогенное жало
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!HasComp<DnaComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        if (DrugSting(uid, target, component))
        {
            args.Handled = true;

            var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageSuccess, uid, uid);
        }

    }

    private void OnAdrenaline(EntityUid uid, ChangelingComponent component, AdrenalineActionEvent args)     // Адреналин
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTen))
            return;

        if (Adrenaline(uid, component))
        {
            args.Handled = true;

            var selfMessage = Loc.GetString("changeling-adrenaline-self-success");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }

    }

    private void OnOmniHeal(EntityUid uid, ChangelingComponent component, OmniHealActionEvent args)     // Флешменд
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
            return;

        if (OmniHeal(uid, component))
        {
            args.Handled = true;

            var selfMessage = Loc.GetString("changeling-omnizine-self-success");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.Small);
        }

    }

    private void OnMuscles(EntityUid uid, ChangelingComponent component, ChangelingMusclesActionEvent args)     // Мускулы
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        if (Muscles(uid, component))
        {
            args.Handled = true;

            var message = Loc.GetString("changeling-muscles");
            _popup.PopupEntity(message, uid, uid);
        }

    }

    private void OnLesserForm(EntityUid uid, ChangelingComponent component, ChangelingLesserFormActionEvent args)       // Низшая форма
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        if (LesserForm(uid, component))
        {
            args.Handled = true;
        }

    }
    public ProtoId<DamageGroupPrototype> GibDamageGroup = "Brute";
    private void OnLastResort(EntityUid uid, ChangelingComponent component, LastResortActionEvent args)     // Последний шанс
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostFree))
            return;

        if (SpawnLingSlug(uid, component))
        {
            var damage_brute = new DamageSpecifier(_proto.Index(GibDamageGroup), component.GibDamage);
            _damageableSystem.TryChangeDamage(uid, damage_brute);

            args.Handled = true;
        }
    }

    private void OnBiodegrade(EntityUid uid, ChangelingComponent component, LingBiodegradeActionEvent args)
    {
        if (args.Handled)
            return;

        if (_mobState.IsDead(uid))
        {
            var selfMessage = Loc.GetString("changeling-regenerate-fail-dead");
            _popup.PopupEntity(selfMessage, uid, uid);
        }

        if (!TryComp<CuffableComponent>(uid, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
        {
            var selfMessage = Loc.GetString("changeling-biodegrade-fail-nocuffs");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostFifteen))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("changeling-biodegrade-start"), uid, uid);

        var doAfter = new DoAfterArgs(EntityManager, uid, component.BiodegradeDuration, new BiodegradeDoAfterEvent(), uid, target: uid)
        {
            DistanceThreshold = 2,
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            RequireCanInteract = false
        };

    _doAfter.TryStartDoAfter(doAfter);
    }
    private void OnBiodegradeDoAfter(EntityUid uid, ChangelingComponent component, BiodegradeDoAfterEvent args)     // DoAfter, та полоска над персонажем
    {
        if (args.Handled || args.Args.Target == null)
            return;
        args.Handled = true;
        var target = args.Args.Target.Value;
        if (args.Cancelled)
        {
            var selfMessage = Loc.GetString("changeling-biodegrade-interrupted");
            _popup.PopupEntity(selfMessage, uid, uid);
            args.Repeat = false;
            return;
        }
        if (!TryComp<CuffableComponent>(target, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;
        if (!TryComp<HandcuffComponent>(cuffs.LastAddedCuffs, out var handcuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;
        _cuffable.Uncuff(target, uid, cuffs.LastAddedCuffs, cuffs, handcuffs);
    }

    public void OnResonantShriek(EntityUid uid, ChangelingComponent component, LingResonantShriekEvent args)
    {
        if (args.Handled)
            return;

        if (_mobState.IsDead(uid))
        {
            var selfMessage = Loc.GetString("changeling-regenerate-fail-dead");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        if (ResonantShriek(uid, component))
        {
            args.Handled = true;
        }
    }

    public void OnTransformSting(EntityUid uid, ChangelingComponent component, TransformationStingEvent args)
    {
        var selectedHumanoidData = component.StoredDNA[component.SelectedDNA];

        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        var dnaComp = EnsureComp<DnaComponent>(target);

        if (selectedHumanoidData.DNA == dnaComp.DNA)
        {
            var selfMessage = Loc.GetString("changeling-transform-sting-fail-already", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
        }

        else if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessageFailNoHuman = Loc.GetString("changeling-transform-sting-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessageFailNoHuman, uid, uid);
            return;
        }

        else if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-transform-sting-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        else
        {
            if (!TryUseAbility(uid, component, component.ChemicalsCostFifty))
                return;

            args.Handled = true;

            if (selectedHumanoidData.EntityUid == null)
                return;

            var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(selectedHumanoidData.EntityUid.Value);
            if (newHumanoidData == null)
                return;

            var transformedUid = _polymorph.PolymorphEntityAsHumanoid(target, newHumanoidData.Value);
            if (transformedUid == null)
                return;

            if (selectedHumanoidData.MetaDataComponent != null)
            {
                var selfMessage = Loc.GetString("changeling-transform-sting-activate", ("target", selectedHumanoidData.MetaDataComponent.EntityName));
                _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);
            }

            if (!TryComp(transformedUid.Value, out InventoryComponent? inventory))
                return;
        }
    }

}
