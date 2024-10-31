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
using Content.Shared.Store.Components;
using Content.Server.Body.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Whitelist;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    private void InitializeUsefulAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, LingAbsorbActionEvent>(StartAbsorbing);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, LingRegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, LingInvisibleActionEvent>(OnLingInvisible);
        SubscribeLocalEvent<ChangelingComponent, LingStingExtractActionEvent>(OnLingDNASting);
        SubscribeLocalEvent<ChangelingComponent, StasisDeathActionEvent>(OnStasisDeathAction);
        SubscribeLocalEvent<ChangelingComponent, FleshmendActionEvent>(OnFleshmend);
        SubscribeLocalEvent<ChangelingComponent, MuteStingEvent>(OnMuteSting);
        SubscribeLocalEvent<ChangelingComponent, DrugStingEvent>(OnDrugSting);
        SubscribeLocalEvent<ChangelingComponent, ChangelingLesserFormActionEvent>(OnLesserForm);
        SubscribeLocalEvent<ChangelingComponent, LastResortActionEvent>(OnLastResort);
        SubscribeLocalEvent<ChangelingComponent, LingBiodegradeActionEvent>(OnBiodegrade);
        SubscribeLocalEvent<ChangelingComponent, BiodegradeDoAfterEvent>(OnBiodegradeDoAfter);
        SubscribeLocalEvent<ChangelingComponent, TransformationStingEvent>(OnTransformSting);
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

    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDoAfterEvent args)
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
            if (TryComp<DnaComponent>(target, out var dnaCompTarget))
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
            var dmg = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Genetic"), component.AbsorbGeneticDmg);
            _damageableSystem.TryChangeDamage(target, dmg);
            _bloodstreamSystem.ChangeBloodReagent(target, "FerrochromicAcid");
            _bloodstreamSystem.SpillAllSolutions(target);
            EnsureComp<AbsorbedComponent>(target);

            if (HasComp<ChangelingComponent>(target)) // Если это был другой генокрад, получим моментально 5 очков эволюции
            {
                var selfMessage = Loc.GetString("changeling-dna-success-ling", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);

                if (TryComp<StoreComponent>(uid, out var store))
                {
                    _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "EvolutionPoints", component.AbsorbedChangelingPointsAmount } }, uid, store);
                    _store.UpdateUserInterface(uid, uid, store);
                }
            }
            else  // Если это не был генокрад, получаем возможность "сброса"
            {
                var selfMessage = Loc.GetString("changeling-dna-success", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(selfMessage, uid, uid, PopupType.Medium);
                component.CanRefresh = true;
                component.AbsorbedDnaModifier = component.AbsorbedDnaModifier + 1;
            }
        }

        if (component.AbsorbStage >= 2)
            component.AbsorbStage = 0;
        else
            component.AbsorbStage += 1;
    }

    private static bool RepeatDoAfter(ChangelingComponent component)
    {
        if (component.AbsorbStage < 2.0)
            return true;
        else
            return false;
    }

    private void OnRegenerate(EntityUid uid, ChangelingComponent component, LingRegenerateActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-lesser-form"), uid, uid);
            return;
        }

        if (!_mobState.IsCritical(uid)) // make sure the ling is critical, if not they cant regenerate
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerate-fail-not-crit"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTen))
            return;

        args.Handled = true;

        var damage_brute = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), component.RegenerateBurnHealAmount);
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

    private void OnLingInvisible(EntityUid uid, ChangelingComponent component, LingInvisibleActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }
        if (component.SiliconStealthEnabled)
        {
            var selfMessage = Loc.GetString("changeling-chameleon-fail-silicon-stealth-active");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive, !component.ChameleonSkinActive))
            return;

        args.Handled = true;

        var stealth = EnsureComp<StealthComponent>(uid);
        var stealthonmove = EnsureComp<StealthOnMoveComponent>(uid);

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

    private void OnLingDNASting(EntityUid uid, ChangelingComponent component, LingStingExtractActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

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

    private void OnStasisDeathAction(EntityUid uid, ChangelingComponent component, StasisDeathActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        component.StasisDeathActive = !component.StasisDeathActive;

        if (component.StasisDeathActive)
        {
            if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
                return;

            args.Handled = true;

            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                mind.PreventGhosting = true;

            _mobState.ChangeMobState(uid, MobState.Dead);

            var selfMessage = Loc.GetString("changeling-stasis-death-self-success");  /// всё, я спать откисать, адьос
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            if (!_mobState.IsDead(uid))
            {
                component.StasisDeathActive = false;
                return;
            }

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

        _action.SetToggled(component.ChangelingStasisDeathActionEntity, component.StasisDeathActive);
    }

    private void OnMuteSting(EntityUid uid, ChangelingComponent component, MuteStingEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        _status.TryAddStatusEffect(target, "Muted", TimeSpan.FromSeconds(45), true);

        var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessageSuccess, uid, uid);

    }

    private void OnDrugSting(EntityUid uid, ChangelingComponent component, DrugStingEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        _hallucinations.StartHallucinations(target, "ADTHallucinations", TimeSpan.FromSeconds(40), true, "Changeling");
        var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessageSuccess, uid, uid);

    }

    private void OnFleshmend(EntityUid uid, ChangelingComponent component, FleshmendActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-lesser-form"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwentyFive))
            return;

        args.Handled = true;

        var damage_brute = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(uid, damage_brute);
        _damageableSystem.TryChangeDamage(uid, damage_burn);
        _bloodstreamSystem.TryModifyBloodLevel(uid, component.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
        _bloodstreamSystem.TryModifyBleedAmount(uid, component.RegenerateBleedReduceAmount);
        _audioSystem.PlayPvs(component.SoundFleshQuiet, uid);

        var selfMessage = Loc.GetString("changeling-omnizine-self-success");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.Small);
    }

    private void OnLesserForm(EntityUid uid, ChangelingComponent component, ChangelingLesserFormActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        RemoveShieldEntity(uid, component);
        RemoveBladeEntity(uid, component);
        RemCompDeferred<StealthComponent>(uid);
        RemCompDeferred<StealthOnMoveComponent>(uid);
        component.MusclesActive = false;
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);

        if (component.LingArmorActive)
        {
            _inventorySystem.TryUnequip(uid, "head", true, true, false);
            _inventorySystem.TryUnequip(uid, "outerClothing", true, true, false);
            component.ChemicalsPerSecond += component.LingArmorRegenCost;
        }

        component.LesserFormActive = !component.LesserFormActive;
        foreach (var item in component.BoughtActions)
        {
            if (!item.HasValue)
                continue;

            if (TryPrototype(item.Value, out var proto) && proto.ID == "ActionLingLesserForm")
                _action.SetToggled(item, component.LesserFormActive);
        }
        args.Handled = true;

        if (component.LesserFormActive)
        {
            var transformedUid = _polymorph.PolymorphEntity(uid, component.LesserFormMob);
            if (transformedUid == null)
                return;

            var selfMessage = Loc.GetString("changeling-lesser-form-activate-monkey");
            _popup.PopupEntity(selfMessage, transformedUid.Value, transformedUid.Value);

            CopyLing(uid, transformedUid.Value);
        }
        else
        {
            if (TryComp<ActorComponent>(uid, out var actorComponent))
            {
                var ev = new RequestChangelingFormsMenuEvent(GetNetEntity(uid), ChangelingMenuType.HumanForm);

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
        }
    }

    private void OnLastResort(EntityUid uid, ChangelingComponent component, LastResortActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostFree))
            return;
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        args.Handled = true;

        var slug = Spawn("ADTChangelingHeadslug", Transform(uid).Coordinates);
        _mindSystem.TransferTo(mindId, slug);

        _gib.TryGibEntity(uid, uid, GibType.Gib, GibContentsOption.Drop, out _);
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
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            RequireCanInteract = false
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnBiodegradeDoAfter(EntityUid uid, ChangelingComponent component, BiodegradeDoAfterEvent args)
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
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostFifty))
            return;

        args.Handled = true;

        if (TryComp<ActorComponent>(uid, out var actorComponent))
        {
            var ev = new RequestChangelingFormsMenuEvent(GetNetEntity(target), ChangelingMenuType.Sting);

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
    }
}
