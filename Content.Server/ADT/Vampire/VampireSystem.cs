// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Humanoid.Components;
using Content.Server.NPC.HTN;
using Content.Server.NullRod;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bible.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Metabolism;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.NullRod.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Shaders;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Surgery.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Throwing;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private EuiManager _euiMan = default!;
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private MetabolizerSystem _metabolism = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NullDamageSystem _nullDamage = default!;
    [Dependency] private RottingSystem _rotting = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private StomachSystem _stomach = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private VampireRuleSystem _vampireRule = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private const int MaxBloodToConsume = 200;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";
    private static readonly EntProtoId RejuvenateAdvanced = "ActionVampireRejuvenateAdvanced";
    private static readonly EntProtoId RejuvenateNoHeal = "ActionVampireRejuvenateNoHeal";
    private static readonly EntProtoId Clumsy = "StatusEffectClumsyClown";

    /// <summary>
    /// An array of "transparent" coatings that allow sunlight to reach the vampire.
    /// </summary>
    private static readonly ProtoId<ContentTileDefinition>[] FloorProto = new ProtoId<ContentTileDefinition>[]
    {
        "Space", "Lattice", "FloorGlass", "FloorRGlass"
    };

    /// <summary>
    /// An array of "Organic" blood.
    /// </summary>
    private static readonly ProtoId<ReagentPrototype>[] BloodProto = new ProtoId<ReagentPrototype>[]
    {
        "Blood", "CopperBlood", "InsectBlood", "SulfurBlood", "AmmoniaBlood", "ResomiBlood", "AriralBlood"
    };

    public override void Initialize()
    {
        base.Initialize();

        InitializePowers();
        InitializeDiablerie();
        InitializeOrgans();
        InitializeThralls();

        SubscribeLocalEvent<VampireComponent, ComponentAdd>(OnAdd);
        SubscribeLocalEvent<VampireComponent, ComponentRemove>(OnRemove);

        // Select Class
        SubscribeLocalEvent<VampireComponent, VampireSelectClassActionEvent>(SelectClass);

        // Drinking Blood
        SubscribeLocalEvent<VampireComponent, VampireDrinkingBloodActionEvent>(OnDrinkBlood);
        SubscribeLocalEvent<VampireComponent, VampireDrinkingBloodDoAfterEvent>(DrinkDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var vampireQuery = EntityQueryEnumerator<VampireComponent>();
        while (vampireQuery.MoveNext(out var uid, out var vampireComponent))
        {
            if (ShouldTakeSpaceDamage(vampireComponent, frameTime, out var shouldDamage) && shouldDamage)
            {
                if (IsInSpace(uid)) DoSpaceDamage(uid, vampireComponent);
            }
        }
    }

    #region Vampire Processing

    private void OnAdd(Entity<VampireComponent> vampire, ref ComponentAdd args)
    {
        if (HasComp<PolymorphedEntityComponent>(vampire) || HasComp<VampireInferiorComponent>(vampire))
            return;

        var state = EnsureComp<VampireOriginalStateComponent>(vampire);
        SaveOriginalState(vampire, state);

        MakeVampire(vampire);
    }

    private void OnRemove(Entity<VampireComponent> vampire, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(vampire.Owner))
            return;

        if (HasComp<PolymorphedEntityComponent>(vampire))
            return;

        if (TryComp<VampireOriginalStateComponent>(vampire, out var state))
            RestoreOriginalState(vampire, state);

        RemCompDeferred<VampireOriginalStateComponent>(vampire);
    }

    #region Processing Helpers

    private void SaveOriginalState(Entity<VampireComponent> vampire, VampireOriginalStateComponent state)
    {
        var componentsToRemove = new[]
        {
            typeof(PacifiedComponent), typeof(PerishableComponent),
            typeof(BarotraumaComponent), typeof(TemperatureSpeedComponent)
        };

        foreach (var type in componentsToRemove)
        {
            if (HasComp(vampire, type))
            {
                state.RemovedComponents.Add(type);
            }
        }

        _status.TryRemoveStatusEffect(vampire.Owner, Clumsy);
        if (TryComp<BodyComponent>(vampire, out var body) && body.Organs != null)
        {
            foreach (var organ in body.Organs.ContainedEntities)
            {
                if (TryComp<MetabolizerComponent>(organ, out var meta) && meta.MetabolizerTypes != null)
                    state.OriginalMetabolizerTypes[organ] = new(meta.MetabolizerTypes);
            }
        }

        if (TryComp<SatiationComponent>(vampire, out var satiation))
        {
            var thirst = satiation.GetOrNull(SatiationSystem.Thirst);
            if (thirst != null)
            {
                state.OriginalThirst = thirst;
                _satiation.RemoveSatiationType(vampire.Owner, SatiationSystem.Thirst);
            }
        }

        if (TryComp<TemperatureDamageComponent>(vampire, out var temp))
            state.OriginalColdDamageThreshold = temp.ColdDamageThreshold;

        if (TryComp<DamageableComponent>(vampire, out var dmg))
            state.OriginalDamageModifierSetId = dmg.DamageModifierSetId;

        state.OriginalEyeColor = GetCurrentEyeColor(vampire);
    }

    private void MakeVampire(Entity<VampireComponent> vampire, bool inferior = false)
    {
        var toRemove = new[]
        {
            typeof(PacifiedComponent), typeof(PerishableComponent),
            typeof(BarotraumaComponent), typeof(TemperatureSpeedComponent)
        };

        foreach (var type in toRemove)
        {
            if (HasComp(vampire, type))
                RemComp(vampire, type);
        }

        if (TryComp<BodyComponent>(vampire, out var body) && body.Organs != null)
        {
            foreach (var organ in body.Organs.ContainedEntities)
            {
                if (TryComp<MetabolizerComponent>(organ, out var meta))
                {
                    if (HasComp<LungComponent>(organ)) // Микола, я трохи "I can't breathe".
                        continue;

                    _metabolism.ClearMetabolizerTypes(meta);
                    _metabolism.TryAddMetabolizerType(organ, VampireComponent.MetabolizerVampire);
                }
            }
        }

        if (TryComp<TemperatureDamageComponent>(vampire, out var temp))
            temp.ColdDamageThreshold = Atmospherics.TCMB;

        EnsureComp<UnholyComponent>(vampire);
        _damage.SetDamageModifierSetId(vampire.Owner, VampireComponent.VampireDamageModifier);

        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            reactive.ReactiveGroups ??= new();
            if (!reactive.ReactiveGroups.ContainsKey("Unholy"))
                reactive.ReactiveGroups.Add("Unholy", new() { ReactionMethod.Touch });
        }

        SetEyeColor(vampire.Owner, Color.FromHex("#E22218FF"));

        vampire.Comp.DrinkActionEntity = _action.AddAction(vampire, VampireComponent.DrinkActionPrototype);
        vampire.Comp.RejuvenateActionEntity = _action.AddAction(vampire, VampireComponent.RejuvenateActionPrototype);
        vampire.Comp.GlareActionEntity = _action.AddAction(vampire, VampireComponent.GlareActionPrototype);

        if (!inferior)
        {
            vampire.Comp.SelectClassActionEntity = _action.AddAction(vampire, VampireComponent.SelectClassActionPrototype);
        }

        _alerts.ShowAlert(vampire.Owner, vampire.Comp.BloodAlert);
        _vampireRule.InitVampireRecord(vampire);
    }

    private void RestoreOriginalState(Entity<VampireComponent> vampire, VampireOriginalStateComponent state)
    {
        foreach (var type in state.RemovedComponents)
        {
            if (!HasComp(vampire, type))
            {
                AddComp(vampire, _componentFactory.GetComponent(type));
            }
        }

        var toRemove = new[]
        {
            typeof(SupremeVampireComponent), typeof(ThrallOwnerComponent), typeof(BestiaContainerComponent)
        };

        foreach (var type in toRemove)
        {
            if (HasComp(vampire, type))
                RemComp(vampire, type);
        }

        foreach (var (organ, types) in state.OriginalMetabolizerTypes)
        {
            if (TryComp<MetabolizerComponent>(organ, out var meta))
            {
                _metabolism.ClearMetabolizerTypes(meta);
                foreach (var type in types)
                {
                    _metabolism.TryAddMetabolizerType(organ, type);
                }
            }
        }

        if (state.OriginalThirst != null && HasComp<SatiationComponent>(vampire))
            _satiation.AddSatiation(vampire.Owner, SatiationSystem.Thirst, state.OriginalThirst);

        if (state.OriginalColdDamageThreshold is float cold && TryComp<TemperatureDamageComponent>(vampire, out var temp))
            temp.ColdDamageThreshold = cold;

        _damage.SetDamageModifierSetId(vampire.Owner, state.OriginalDamageModifierSetId);

        if (state.OriginalEyeColor is Color color)
            SetEyeColor(vampire.Owner, color);

        RemComp<UnholyComponent>(vampire);
        RemComp<VampireInferiorComponent>(vampire);
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
            reactive.ReactiveGroups?.Remove("Unholy");

        foreach (var (_, actionEntity) in vampire.Comp.AcquiredSkills)
        {
            if (actionEntity != null)
            {
                _action.RemoveAction(vampire.Owner, actionEntity.Value);
            }
        }
        vampire.Comp.AcquiredSkills.Clear();

        _action.RemoveAction(vampire.Owner, vampire.Comp.DrinkActionEntity);
        _action.RemoveAction(vampire.Owner, vampire.Comp.SelectClassActionEntity);
        _action.RemoveAction(vampire.Owner, vampire.Comp.RejuvenateActionEntity);
        _action.RemoveAction(vampire.Owner, vampire.Comp.GlareActionEntity);

        _alerts.ClearAlert(vampire.Owner, vampire.Comp.BloodAlert);
    }

    #endregion Processing Helpers

    #endregion Vampire Processing

    #region Select Class

    private void SelectClass(EntityUid uid, VampireComponent component, VampireSelectClassActionEvent args)
    {
        if (HasComp<VampireInferiorComponent>(uid))
            return;

        if (component.CurrentBlood < args.BloodCost)
        {
            _popup.PopupEntity(Loc.GetString("vampire-hungry"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (_mind.TryGetMind(uid, out _, out var mind) && mind.UserId is { } userId)
        {
            var eui = new VampireClassSelectionEui(uid, this);
            _euiMan.OpenEui(eui, _player.GetSessionById(userId));
        }

        args.Handled = true;
    }

    public void OnClassSelected(EntityUid uid, VampireClassEnum selectedClass, VampireComponent? vampire = null)
    {
        if (!Resolve(uid, ref vampire)) // It's like, "Hey man, what the hell?"
            return;

        // Checking for repeated attempts to change the class.
        if (vampire.CurrentEvolution != VampireClassEnum.NonSelected)
            return;

        _action.RemoveAction(uid, vampire.SelectClassActionEntity);
        vampire.SelectClassActionEntity = null;

        vampire.CurrentEvolution = selectedClass;
        _vampireRule.RecordClassSelected(uid, selectedClass);

        SetEyeColor(uid, GetVampireEyeColor(selectedClass));

        switch (selectedClass)
        {
            case VampireClassEnum.Umbrae:
                {
                    var nightvision = EnsureComp<NaturalNightVisionComponent>(uid);
                    nightvision.VisionRadius = GetNightVisionRadiusForClass(selectedClass);
                    nightvision.TintColor = GetNightVisionColorForClass(selectedClass);
                    Dirty(uid, nightvision);
                    break;
                }

            case VampireClassEnum.Gargantua:
                {
                    _action.RemoveAction(uid, vampire.RejuvenateActionEntity);
                    vampire.RejuvenateActionEntity = _action.AddAction(uid, RejuvenateAdvanced);
                    break;
                }

            case VampireClassEnum.Hemomancer:
                {
                    _action.RemoveAction(uid, vampire.RejuvenateActionEntity);
                    vampire.RejuvenateActionEntity = _action.AddAction(uid, RejuvenateNoHeal);
                    break;
                }

            case VampireClassEnum.Dantalion:
                {
                    EnsureComp<ThrallOwnerComponent>(uid);
                    break;
                }

            case VampireClassEnum.Bestia:
                {
                    EnsureComp<BestiaContainerComponent>(uid);
                    break;
                }

            default: break;
        }

        UpdatePowers((uid, vampire));
    }

    #endregion

    #region Drinking Blood

    private void OnDrinkBlood(Entity<VampireComponent> ent, ref VampireDrinkingBloodActionEvent args)
    {
        if (TryDrink(ent, args.Target))
        {
            var baseDelay = args.Delay;

            var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);
            var reduction = kidneysCount * 0.3;

            var totalSeconds = Math.Max(0.5, baseDelay.TotalSeconds - reduction);
            var finalDelay = TimeSpan.FromSeconds(totalSeconds);

            var doAfterEventArgs = new DoAfterArgs(EntityManager, ent, finalDelay,
                new VampireDrinkingBloodDoAfterEvent(),
                ent, args.Target, args.Target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                DistanceThreshold = 0.5f,
                NeedHand = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion", ("vampire", Identity.Name(ent, EntityManager, args.Target))),
                args.Target, args.Target, PopupType.MediumCaution);
        }
    }

    private bool TryDrink(Entity<VampireComponent> ent, EntityUid target)
    {
        if (target == ent.Owner)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-self"), ent, ent, PopupType.SmallCaution);
            return false;
        }

        if (!_interaction.InRangeUnobstructed(ent.Owner, target, popup: true) || HasComp<SyntheticOperatedComponent>(target))
            return false;

        IngestionBlockerComponent? blocker;
        if (_inventory.TryGetSlotEntity(ent, "mask", out var maskUid) && TryComp(maskUid, out blocker) && blocker.Enabled)
            return false;

        if (_inventory.TryGetSlotEntity(ent, "head", out var headUid) && TryComp(headUid, out blocker) && blocker.Enabled)
            return false;

        if (_rotting.IsRotten(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), ent, ent, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<ThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-not-thrall"), ent, ent, PopupType.SmallCaution);
            return false;
        }

        if (TryComp<SSDIndicatorComponent>(target, out var targetSSD))
        {
            if (targetSSD.IsSSD && !_mobState.IsDead(target))
            {
                _popup.PopupEntity(Loc.GetString("vampire-blooddrink-ssd"), ent, ent, PopupType.SmallCaution);
                return false;
            }
        }

        if (HasComp<HTNComponent>(target) || HasComp<RandomHumanoidAppearanceComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-not-sentient"), ent, ent, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void DrinkDoAfter(EntityUid uid, VampireComponent component, ref VampireDrinkingBloodDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        var target = args.Target.Value;
        if (!TryComp<BloodstreamComponent>(target, out var targetBloodstream) || targetBloodstream.BloodSolution == null)
            return;

        if (_rotting.IsRotten(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted", ("target", Identity.Name(target, EntityManager))),
                uid, uid, PopupType.MediumCaution);
            return;
        }

        var victimBloodRemaining = targetBloodstream.BloodSolution.Value.Comp.Solution.Volume;
        if (victimBloodRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var bloodAlreadyConsumed = GetBloodConsumedByVampire(uid, target);
        var maxAvailableBlood = (FixedPoint2)Math.Min((float)victimBloodRemaining, (float)(MaxBloodToConsume - bloodAlreadyConsumed));
        if (maxAvailableBlood <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-maxed-out"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var diablerieBonus = TryComp<VampireDiablerieComponent>(uid, out var diablerie)
            ? diablerie.DiablerieLevel * diablerie.SuckingBonusPerLevel : 0f;
        var volumeToConsume = (FixedPoint2)Math.Min(victimBloodRemaining.Value, args.Volume + diablerieBonus);
        var volumeToStomach = volumeToConsume * args.AbsorptionRatio;
        var volumeToEssence = volumeToConsume * 0.95;

        _audio.PlayPvs(component.BloodDrainSound, uid);
        _blood.TryModifyBloodLevel(target, -volumeToConsume);
        EnsureComp<BittenByVampireComponent>(target);

        if (TryComp<VampireComponent>(target, out var targetVampire))
        {
            args.Repeat = TryPerformDiablerie((uid, component), (target, targetVampire), volumeToEssence);
            return;
        }

        if (HasComp<BibleUserComponent>(target) && !HasTruePower(uid))
        {
            _damage.TryChangeDamage(uid, component.HolyDamage, true);
            _popup.PopupEntity(Loc.GetString("vampire-ingest-holyblood"), uid, uid, PopupType.LargeCaution);
            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(uid):user} attempted to drink {volumeToConsume}u of {ToPrettyString(target):target}'s holy blood");
            return;
        }

        var bloodSolution = _solution.SplitSolution(targetBloodstream.BloodSolution.Value, volumeToStomach);
        if (volumeToStomach > 0)
        {
            if (!TryIngestBlood(uid, bloodSolution))
            {
                _solution.AddSolution(targetBloodstream.BloodSolution.Value, bloodSolution);
                return;
            }
        }

        _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(uid):user} drank {volumeToConsume}u of {ToPrettyString(target):target}'s blood ({volumeToStomach}u to stomach)");

        if (HasComp<HumanoidProfileComponent>(target) && !HasComp<DnaModifiedComponent>(target))
            AddBloodEssence(uid, volumeToEssence);

        SetBloodConsumedByVampire(uid, target, bloodAlreadyConsumed + volumeToConsume);
        _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion-doafter"), target, target, PopupType.SmallCaution);

        args.Repeat = true;
    }

    private bool TryIngestBlood(EntityUid uid, Solution ingestedSolution)
    {
        if (TryComp<BodyComponent>(uid, out var body) && body.Organs != null)
        {
            var stomachs = new List<EntityUid>();
            foreach (var organ in body.Organs.ContainedEntities)
            {
                if (HasComp<StomachComponent>(organ))
                    stomachs.Add(organ);
            }

            foreach (var stomach in stomachs)
            {
                if (_stomach.CanTransferSolution(stomach, ingestedSolution))
                    return _stomach.TryTransferSolution(stomach, ingestedSolution);
            }

            _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        return false;
    }

    private FixedPoint2 GetBloodConsumedByVampire(Entity<VampireComponent?> vampire, EntityUid target)
    {
        if (!Resolve(vampire, ref vampire.Comp, false))
            return FixedPoint2.Zero;

        return vampire.Comp.BloodConsumedFromVictim.GetValueOrDefault(target, FixedPoint2.Zero);
    }

    private void SetBloodConsumedByVampire(Entity<VampireComponent?> vampire, EntityUid target, FixedPoint2 amount)
    {
        if (!Resolve(vampire, ref vampire.Comp, false) || amount <= 0)
            return;

        vampire.Comp.BloodConsumedFromVictim[target] = amount;
    }

    #endregion

    #region Blood Manipulation

    private bool AddBloodEssence(Entity<VampireComponent?> vampire, FixedPoint2 quantity)
    {
        if (!Resolve(vampire, ref vampire.Comp, false) || quantity <= 0)
            return false;

        var stomachCount = GetOrganTypeCount(vampire.Owner, BestiaOrganType.Stomach);
        var bonus = stomachCount * 0.25;
        quantity += bonus;

        if (TryAddBlood(vampire.Comp, quantity, out _))
        {
            Dirty(vampire.Owner, vampire.Comp);
            _alerts.UpdateAlert(vampire.Owner, vampire.Comp.BloodAlert);
            UpdatePowers(vampire);

            _vampireRule.RecordBloodDrank(vampire.Owner, quantity);
            return true;
        }

        return false;
    }

    private bool SubtractBloodEssence(Entity<VampireComponent?> vampire, FixedPoint2 quantity)
    {
        if (!Resolve(vampire, ref vampire.Comp, false) || quantity <= 0)
            return false;

        var nullDamage = _nullDamage.GetNullDamage(vampire);
        if (TrySubtractBlood(vampire.Comp, quantity, nullDamage))
        {
            Dirty(vampire.Owner, vampire.Comp);
            _alerts.UpdateAlert(vampire.Owner, vampire.Comp.BloodAlert);
            return true;
        }

        return false;
    }

    private bool CheckBloodEssence(Entity<VampireComponent?> vampire, FixedPoint2 quantity)
    {
        if (!Resolve(vampire, ref vampire.Comp, false))
            return false;

        var nullDamage = _nullDamage.GetNullDamage(vampire);
        return CheckBloodEssence(vampire.Comp, quantity, nullDamage);
    }

    private void UpdatePowers(Entity<VampireComponent?> vampire)
    {
        if (!Resolve(vampire, ref vampire.Comp, false))
            return;

        if (vampire.Comp.CurrentEvolution == VampireClassEnum.NonSelected)
            return;

        if (vampire.Comp.CurrentEvolution == VampireClassEnum.Dantalion)
            UpdateThrallCount(vampire);

        if (vampire.Comp.CurrentEvolution == VampireClassEnum.Bestia)
            UpdateBestiaLimits(vampire);

        var newSkills = GetNewSkillsToAdd(vampire.Comp);

        foreach (var skillProto in newSkills)
        {
            AddSkill(vampire, vampire.Comp, skillProto);
            _admin.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(vampire)}: added {skillProto} for {vampire.Comp.CurrentEvolution}.");
        }

        if (ShouldHaveTruePower(vampire.Comp.CurrentBlood) && !HasTruePower(vampire))
            MakeImmuneToHoly((vampire.Owner, vampire.Comp));

        Dirty(vampire.Owner, vampire.Comp);
    }

    private void AddSkill(EntityUid uid, VampireComponent component, EntProtoId skillProto)
    {
        if (HasSkill(component, skillProto))
            return;

        if (skillProto == "ActionVampireBloodSwellAdvanced") // Replace BloodSwell
        {
            if (component.AcquiredSkills.TryGetValue("ActionVampireBloodSwell", out var oldAction) && oldAction != null)
            {
                _action.RemoveAction(uid, oldAction.Value);
                component.AcquiredSkills["ActionVampireBloodSwell"] = null;
            }
        }

        var actionEntity = _action.AddAction(uid, skillProto);
        if (actionEntity != null) component.AcquiredSkills[skillProto] = actionEntity;
    }

    #endregion

    #region Space Damage

    private void DoSpaceDamage(EntityUid uid, VampireComponent component)
    {
        _damage.TryChangeDamage(uid, component.SpaceDamage, true);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), uid, uid, PopupType.LargeCaution);
    }

    private bool IsInSpace(EntityUid vampireUid)
    {
        var vampirePosition = _transform.GetMapCoordinates(Transform(vampireUid));
        if (!_map.TryFindGridAt(vampirePosition, out var gridUid, out var grid))
            return true;

        if (!_map.TryGetTileRef(gridUid, grid, vampirePosition.Position, out var tileRef))
            return true;

        var tile = _turf.GetContentTileDefinition(tileRef);
        if (FloorProto.Contains(tile.ID))
            return true;

        return false;
    }

    #endregion

    #region True Power

    private void MakeImmuneToHoly(Entity<VampireComponent> vampire)
    {
        if (HasComp<VampireInferiorComponent>(vampire))
            return;

        if (TryComp<ReactiveComponent>(vampire, out var reactive))
            reactive.ReactiveGroups?.Remove("Unholy");

        EnsureComp<SupremeVampireComponent>(vampire);
        RemComp<NullDamageComponent>(vampire);
        RemComp<UnholyComponent>(vampire);

        var nightvision = EnsureComp<NaturalNightVisionComponent>(vampire);
        nightvision.VisionRadius = GetNightVisionRadiusForClass(vampire.Comp.CurrentEvolution, true);
        nightvision.TintColor = GetNightVisionColorForClass(vampire.Comp.CurrentEvolution);
        Dirty(vampire, nightvision);

        _popup.PopupEntity(Loc.GetString("vampire-true-power"), vampire, vampire, PopupType.Medium);
    }

    #endregion
}
