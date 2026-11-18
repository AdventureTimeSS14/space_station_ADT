// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using System.Numerics;
using Content.Server.Humanoid.Components;
using Content.Server.NPC.HTN;
using Content.Server.Polymorph.Components;
using Content.Shared.ADT.Damage.Components;
using Content.Shared.ADT.Genetics.Components;
using Content.Shared.ADT.NightVision;
using Content.Shared.ADT.Projectiles;
using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Cuffs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SSDIndicator;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SleepingSystem _sleeping = default!;

    private static readonly SoundSpecifier BestiaScream = new SoundPathSpecifier("/Audio/ADT/Vampire/Effects/Vampire/creepyshriek.ogg");

    private void InitializeBestia()
    {
        // Passive
        SubscribeLocalEvent<BestiaContainerComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<BestiaContainerComponent, BeforeStaminaDamageEvent>(OnStaminaDamageModify);

        // Specific Methods
        SubscribeLocalEvent<VampireBloodAbsorptionComponent, MeleeHitEvent>(OnAbsorptionHit);
        SubscribeLocalEvent<VampireCoffinComponent, DestructionAttemptEvent>(OnDestruction);
        SubscribeLocalEvent<VampireCoffinComponent, EntityTerminatingEvent>(OnTerminating);

        // Abilities
        SubscribeLocalEvent<VampireComponent, VampireCheckTrophiesActionEvent>(OnCheckTrophies);
        SubscribeLocalEvent<VampireComponent, VampireDissectActionEvent>(OnDissect);
        SubscribeLocalEvent<VampireComponent, VampireDissectDoAfterEvent>(OnDissectDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireInfectedTrophyActionEvent>(OnInfectedTrophy);
        SubscribeLocalEvent<VampireComponent, VampireLungeActionEvent>(OnLunge);
        SubscribeLocalEvent<VampireComponent, VampireMarkPreyActionEvent>(OnMarkPrey);
        SubscribeLocalEvent<VampireComponent, VampireMetamorphosisBatsActionEvent>(OnMetamorphosisBats);
        SubscribeLocalEvent<VampireComponent, VampireAnabiosisActionEvent>(OnAnabiosis);
        SubscribeLocalEvent<VampireComponent, VampireSummonBatsActionEvent>(OnSummonBats);
        SubscribeLocalEvent<VampireComponent, VampireMetamorphosisHoundActionEvent>(OnMetamorphosisHound);

        // Polymorph Block
        SubscribeLocalEvent<VampirePolymorphComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<VampirePolymorphComponent, VampireDissectDoAfterEvent>(OnDissectDoAfter);

        SubscribeLocalEvent<VampirePolymorphComponent, VampireResonantShriekActionEvent>(OnResonantShriek);
        SubscribeLocalEvent<VampirePolymorphComponent, VampireLungeFinaleActionEvent>(OnLungeFinale);
    }

    #region Passive

    private void OnDamageModify(Entity<BestiaContainerComponent> ent, ref DamageModifyEvent args)
    {
        var heartCount = GetOrganTypeCount(ent, BestiaOrganType.Heart);
        var lungsCount = GetOrganTypeCount(ent, BestiaOrganType.Lungs);
        var liverCount = GetOrganTypeCount(ent, BestiaOrganType.Liver);
        var kidneysCount = GetOrganTypeCount(ent, BestiaOrganType.Kidneys);

        var maxCritical = ent.Comp.MaxCriticalOrgans;
        var maxRegular = ent.Comp.MaxRegularOrgans;

        var modifiers = new Dictionary<ProtoId<DamageTypePrototype>, float>();

        var physicalMultiplier = CalculateBonusMultiplier(heartCount, maxCritical, 5f);
        if (physicalMultiplier < 1f)
        {
            modifiers["Blunt"] = physicalMultiplier;
            modifiers["Slash"] = physicalMultiplier;
            modifiers["Piercing"] = physicalMultiplier;
        }

        var burnMultiplier = CalculateBonusMultiplier(heartCount, maxCritical, 5f);
        if (burnMultiplier < 1f)
        {
            modifiers["Heat"] = burnMultiplier;
            modifiers["Shock"] = burnMultiplier;
        }

        var airlossMultiplier = CalculateBonusMultiplier(lungsCount, maxCritical, 5f);
        if (airlossMultiplier < 1f)
        {
            modifiers["Asphyxiation"] = airlossMultiplier;
        }

        var poisonMultiplier = CalculateBonusMultiplier(liverCount, maxRegular, 3f);
        if (poisonMultiplier < 1f)
        {
            modifiers["Poison"] = poisonMultiplier;
        }

        var cellularMultiplier = CalculateBonusMultiplier(kidneysCount, maxRegular, 3f);
        if (cellularMultiplier < 1f)
        {
            modifiers["Cellular"] = cellularMultiplier;
        }

        if (modifiers.Count > 0)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, new DamageModifierSet
            {
                Coefficients = modifiers
            });
        }
    }

    private void OnStaminaDamageModify(Entity<BestiaContainerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        var lungsCount = GetOrganTypeCount(ent, BestiaOrganType.Lungs);
        var maxCritical = ent.Comp.MaxCriticalOrgans;

        args.Value *= CalculateBonusMultiplier(lungsCount, maxCritical, 5f);
    }

    #endregion

    #region Specific Methods

    private void OnAbsorptionHit(Entity<VampireBloodAbsorptionComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.BloodStealAmount <= 0)
            return;

        if (args.HitEntities.Count == 0 || !HasComp<VampireComponent>(ent.Comp.VampireOwner))
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (_mobState.IsDead(hitEnt) || HasComp<ThrallComponent>(hitEnt))
                continue;

            if (TryComp<SSDIndicatorComponent>(hitEnt, out var hitSSD))
            {
                if (hitSSD.IsSSD && !_mobState.IsDead(hitEnt))
                    continue;
            }

            if (HasComp<HTNComponent>(hitEnt) || HasComp<RandomHumanoidAppearanceComponent>(hitEnt))
                continue;

            if (!HasComp<BloodstreamComponent>(hitEnt))
                continue;

            AddBloodEssence(ent.Comp.VampireOwner, ent.Comp.BloodStealAmount);
        }
    }

    private void OnDestruction(EntityUid uid, VampireCoffinComponent coffin, DestructionAttemptEvent args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        var coordinates = Transform(uid).Coordinates;

        _audio.PlayPvs(BestiaScream, coordinates);
        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(coordinates, 5f)
            .Where(entity => !_mobState.IsDead(entity.Owner))
            .ToList();

        foreach (var entity in nearbyEntities)
        {
            if (HasComp<VampireComponent>(entity.Owner))
                continue;

            _stun.TryUpdateParalyzeDuration(entity.Owner, TimeSpan.FromSeconds(2f));
        }

        // It blocks and deletes because regular destruction empties the container
        // and prevents the vampire from properly emerging from the coffin.
        args.Cancel();
        QueueDel(uid);
    }

    private void OnTerminating(Entity<VampireCoffinComponent> coffin, ref EntityTerminatingEvent args)
    {
        if (!TryComp<EntityStorageComponent>(coffin, out var storage) || storage.Contents.ContainedEntities.Count == 0)
            return;

        var entities = storage.Contents.ContainedEntities.ToList();
        foreach (var contentsEnt in entities)
        {
            _container.Remove(contentsEnt, storage.Contents);
            _status.TryRemoveStatusEffect(contentsEnt, ForceSleeping);
            _sleeping.TryWaking(contentsEnt, true);
        }
    }

    #endregion

    #region Abilities

    private void OnCheckTrophies(Entity<VampireComponent> ent, ref VampireCheckTrophiesActionEvent args)
    {
        var state = GetTrophiesState(ent.Owner);
        if (state != null && _mind.TryGetMind(args.Performer, out _, out var mind) && mind.UserId is { } userId)
        {
            var eui = new TrophiesMenuEui(state);
            _euiMan.OpenEui(eui, _player.GetSessionById(userId));
            eui.StateDirty();
        }

        args.Handled = true;
    }

    private void OnDissect(Entity<VampireComponent> ent, ref VampireDissectActionEvent args)
    {
        var target = args.Target;
        if (!CanDissect(args.Performer, target, out var reason))
        {
            if (reason != null) _popup.PopupEntity(reason, args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        args.Handled = true;

        var availableOrgans = GetAvailableOrgans(ent.Owner, target);
        if (availableOrgans.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-bestia-dissect-no-organs"), args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        if (_mind.TryGetMind(args.Performer, out _, out var mind) && mind.UserId is { } userId)
        {
            var state = new DissectSelectionEuiState(availableOrgans);
            var eui = new DissectSelectionEui(args.Performer, target, args.BloodCost, this, state);
            _euiMan.OpenEui(eui, _player.GetSessionById(userId));
            eui.StateDirty();
        }
    }

    public void StartDissection(EntityUid vampire, EntityUid target, NetEntity selectedOrgan, FixedPoint2 bloodCost)
    {
        var vampireBody = vampire;
        if (TryComp<VampirePolymorphComponent>(vampire, out var polymorph))
            vampireBody = polymorph.Body;

        if (!CanDissectOrgan(vampireBody, target, GetEntity(selectedOrgan), out var reason))
        {
            if (reason != null) _popup.PopupEntity(reason, vampire, vampire, PopupType.SmallCaution);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, vampire, TimeSpan.FromSeconds(5),
            new VampireDissectDoAfterEvent(bloodCost), vampire, target, GetEntity(selectedOrgan))
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 0.5f
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDissectDoAfter<T>(Entity<T> ent, ref VampireDissectDoAfterEvent args) where T : Component
    {
        if (args.Cancelled || args.Target == null || args.Used == null)
            return;

        var user = args.User;
        var vampireBody = user;
        if (TryComp<VampirePolymorphComponent>(user, out var polymorph))
            vampireBody = polymorph.Body;

        var target = args.Target.Value;
        var targetOrgan = args.Used.Value;

        if (!CanDissectOrgan(vampireBody, target, targetOrgan, out var reason))
        {
            if (reason != null) _popup.PopupEntity(reason, user, user, PopupType.SmallCaution);
            return;
        }

        if (!TryExtractOrgan(target, targetOrgan))
        {
            _popup.PopupEntity(Loc.GetString("vampire-bestia-dissect-extract-fail"), user, user, PopupType.SmallCaution);
            return;
        }

        _audio.PlayPvs(args.DissectSound, target);
        _chat.TryEmoteWithoutChat(target, ProtoMan.Index(Scream), true);
        _blood.TryBleedOut(target, args.BloodCost);

        RecordExtraction(vampireBody, target, out var bestia);
        if (bestia != null)
        {
            _container.Insert(targetOrgan, bestia.OrgansContainer);
            UpdateProtections((vampireBody, bestia));
        }

        _popup.PopupEntity(Loc.GetString("vampire-bestia-dissect-success", ("organ", Name(targetOrgan))), user, user, PopupType.Medium);
        SubtractBloodEssence(vampireBody, args.BloodCost);
    }

    private void OnInfectedTrophy(Entity<VampireComponent> ent, ref VampireInfectedTrophyActionEvent args)
    {
        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        if (ent.Owner == args.Target)
            return;

        var targetPos = _transform.GetMapCoordinates(args.Target).Position;
        var shooterPos = _transform.GetMapCoordinates(args.Performer).Position;
        var direction = (targetPos - shooterPos).Normalized();

        var projectile = Spawn(args.ProjectileId, Transform(args.Performer).Coordinates);

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var liverCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Liver);
        var eyesCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Eyes);
        var stomachCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Stomach);

        var damageMultiplier = 1f + (heartCount * 0.5f);
        var radius = CalculateRadius(stomachCount, 0f, 1f, 5f);
        var infectChance = CalculateChance(liverCount, 0f, 0.03f, 0.4f);
        var projectileLifetime = CalculateValue(0f, eyesCount, 0.035f, 0.525f);

        if (TryComp<ProjectileComponent>(projectile, out var projectileComp))
            projectileComp.Damage = projectileComp.Damage * damageMultiplier;

        if (TryComp<ProjectileAoEComponent>(projectile, out var aoEComponent))
            aoEComponent.DamageRadius += radius;

        if (TryComp<ProjectileInfectComponent>(projectile, out var infectComponent))
            infectComponent.Prob = infectChance;

        if (TryComp<TimedDespawnComponent>(projectile, out var timedDespawn))
            timedDespawn.Lifetime = projectileLifetime;

        _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, args.Performer, SharedGunSystem.ProjectileSpeed / 2);

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnLunge(Entity<VampireComponent> ent, ref VampireLungeActionEvent args)
    {
        var performer = args.Performer;
        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(performer);
            return;
        }

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var lungCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Lungs);
        var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);
        var stomachCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Stomach);

        var maxRange = CalculateRange(lungCount, 5f, 1f, 11f);
        var stunDuration = CalculateDuration(heartCount, 0.5f, 0.5f, float.MaxValue);
        var bloodAmount = CalculateValue(kidneysCount, 5, 5f, 50f);
        var radius = CalculateRadius(stomachCount, 0.5f, 0.5f, 1.5f);

        var vampirePos = _transform.GetWorldPosition(performer);
        var targetPos = _transform.ToMapCoordinates(args.Target, true).Position;
        var rawDirection = targetPos - vampirePos;
        var rawDistance = rawDirection.Length();

        var actualDistance = Math.Min(maxRange, rawDistance);
        var direction = rawDirection.Normalized() * actualDistance;

        var throwSpeed = Math.Max(5f, actualDistance / 0.5f);
        _throwing.TryThrow(performer, direction, throwSpeed, compensateFriction: true);

        var flyTime = actualDistance / throwSpeed;
        Timer.Spawn(TimeSpan.FromSeconds(flyTime), () =>
        {
            if (!Exists(performer))
                return;

            foreach (var humanoid in _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(performer).Coordinates, radius))
            {
                if (humanoid.Owner == ent.Owner || humanoid.Owner == performer)
                    continue;

                _blood.TryBleedOut(humanoid.Owner, bloodAmount);
                _stun.TryUpdateParalyzeDuration(humanoid.Owner, TimeSpan.FromSeconds(stunDuration));
            }
        });

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnMarkPrey(Entity<VampireComponent> ent, ref VampireMarkPreyActionEvent args)
    {
        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        var target = args.Target;
        if (ent.Owner == target)
            return;

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);
        var eyesCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Eyes);

        var maxRange = CalculateRange(eyesCount, 3f, 0.5f, 8f);

        var userPos = _transform.GetWorldPosition(ent);
        var targetPos = _transform.GetWorldPosition(target);
        var distance = (targetPos - userPos).Length();
        if (distance > maxRange)
            return;

        var markDuration = CalculateDuration(kidneysCount, 5f, 1f, 15f);
        var burnChance = CalculateChance(heartCount, 0f, 0.1f, 0.6f);
        var burnCount = CalculateDamageBonus(heartCount, 0f, 1f, 6f);

        _movementMod.TryUpdateMovementSpeedModDuration(target, SlowdownStatusEffect, TimeSpan.FromSeconds(markDuration), 0.5f);
        ExecuteMarkPreyBurnTick(ent.Owner, target, 0, (int)markDuration, burnChance, burnCount, args.DamageType);

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnMetamorphosisBats(Entity<VampireComponent> ent, ref VampireMetamorphosisBatsActionEvent args)
    {
        if (HasComp<PolymorphedEntityComponent>(args.Performer))
        {
            var coordinates = Transform(args.Performer).Coordinates;
            Spawn(args.MistReappearEffect, coordinates);

            _polymorph.Revert(args.Performer);
            args.Handled = true;
            return;
        }

        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (_cuffable.TryGetLastCuff(ent.Owner, out var cuffs))
            _cuffable.Uncuff(ent.Owner, null, cuffs.Value);

        var bats = _polymorph.PolymorphEntity(ent, args.PolymorphProto);
        if (bats == null)
            return;

        Spawn(args.MistEffect, Transform(bats.Value).Coordinates);
        ConfiguratePolymorph(ent, bats.Value);

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var lungsCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Lungs);
        var liverCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Liver);
        var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);

        var maxHealth = CalculateHealth(heartCount, 130f, 20f, 250f);
        var bonusDamage = CalculateDamageBonus(heartCount, 0f, 0.75f, 8f);
        var speedMultiplier = CalculateSpeedMultiplier(lungsCount, 1f, 0.05f, 1.3f);
        var leechBlood = CalculateValue(liverCount, 0, 0.5f, 3f);
        var healAmount = CalculateValue(kidneysCount, 1, 1f, 10f);

        _threshold.SetMobStateThreshold(bats.Value, maxHealth, MobState.Dead);

        if (TryComp(bats, out MeleeWeaponComponent? weapon))
        {
            var damageDict = weapon.Damage.DamageDict;
            var damageValue = (int)Math.Round(bonusDamage);

            if (damageDict.ContainsKey(args.BonusDamageType))
                damageDict[args.BonusDamageType] += damageValue;
            else
                damageDict[args.BonusDamageType] = damageValue;
        }

        if (TryComp(bats, out MovementSpeedModifierComponent? speed))
        {
            _speed.ChangeBaseSpeed(bats.Value,
                speed.BaseWalkSpeed * speedMultiplier,
                speed.BaseSprintSpeed * speedMultiplier,
                speed.BaseAcceleration);
        }

        if (TryComp(bats, out LeechMeleeWeaponComponent? leech))
        {
            if (leech.Heal != null) leech.Heal *= healAmount;
            if (leech.HealGroups != null) leech.HealGroups *= healAmount;
        }

        var absorption = EnsureComp<VampireBloodAbsorptionComponent>(bats.Value);
        absorption.VampireOwner = ent.Owner;
        absorption.BloodStealAmount = leechBlood;

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnAnabiosis(Entity<VampireComponent> ent, ref VampireAnabiosisActionEvent args)
    {
        if (HasComp<PolymorphedEntityComponent>(args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("vampire-bestia-anabiosis-polymorphed"), args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var coffin = Spawn(args.CoffinProto, Transform(ent).Coordinates);
        if (!TryComp<EntityStorageComponent>(coffin, out var storage))
        {
            Del(coffin);
            return;
        }

        // For 30 seconds, his heal is already very strong, so I don't see the point in enhancing it here.

        _container.Insert(ent.Owner, storage.Contents, force: true);
        _status.TryAddStatusEffectDuration(ent.Owner, ForceSleeping, out _, args.Duration);

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnSummonBats(Entity<VampireComponent> ent, ref VampireSummonBatsActionEvent args)
    {
        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        var bestiaContainer = Comp<BestiaContainerComponent>(ent.Owner);
        var totalTrophies = bestiaContainer.OrgansContainer.ContainedEntities.Count;

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var lungsCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Lungs);
        var liverCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Liver);
        var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);

        var batCount = CalculateCountFromTrophies(totalTrophies, 1, 20, 4);

        var batHealth = CalculateHealth(heartCount, 80f, 10f, 140f);
        var batDamageBonus = CalculateDamageBonus(heartCount, 0f, 0.75f, 6f);
        var batSpeedMultiplier = CalculateSpeedMultiplier(lungsCount, 1f, 0.1f, 1.6f);
        var bloodPerBite = CalculateValue(liverCount, 0, 0.5f, 10f);

        _audio.PlayPvs(args.Sound, args.Performer);

        for (int i = 0; i < batCount; i++)
        {
            Vector2 offset = new Vector2(_random.NextFloat(-1.5f, 1.5f), _random.NextFloat(-1.5f, 1.5f));
            var spawnPos = Transform(args.Performer).Coordinates.Offset(offset);
            var bat = Spawn(args.BatsProto, spawnPos);

            _threshold.SetMobStateThreshold(bat, batHealth, MobState.Dead);

            if (TryComp<MeleeWeaponComponent>(bat, out var weapon))
            {
                var damageDict = weapon.Damage.DamageDict;
                var damageValue = (int)Math.Round(batDamageBonus);

                if (damageDict.ContainsKey(args.BonusDamageType))
                    damageDict[args.BonusDamageType] += damageValue;
                else
                    damageDict[args.BonusDamageType] = damageValue;
            }

            if (TryComp<MovementSpeedModifierComponent>(bat, out var speed))
            {
                _speed.ChangeBaseSpeed(bat,
                    speed.BaseWalkSpeed * batSpeedMultiplier,
                    speed.BaseSprintSpeed * batSpeedMultiplier,
                    speed.BaseAcceleration);
            }

            var vampireBite = EnsureComp<VampireBloodAbsorptionComponent>(bat);
            vampireBite.VampireOwner = ent.Owner;
            vampireBite.BloodStealAmount = bloodPerBite;

            if (TryComp<LeechMeleeWeaponComponent>(bat, out var leech))
            {
                if (leech.Heal != null) leech.Heal *= kidneysCount;
                if (leech.HealGroups != null) leech.HealGroups *= kidneysCount;
            }
        }

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    private void OnMetamorphosisHound(Entity<VampireComponent> ent, ref VampireMetamorphosisHoundActionEvent args)
    {
        if (HasComp<PolymorphedEntityComponent>(args.Performer))
        {
            var coordinates = Transform(args.Performer).Coordinates;
            Spawn(args.MistReappearEffect, coordinates);

            _polymorph.Revert(args.Performer);
            args.Handled = true;
            return;
        }

        var bloodCost = GetAdjustedBloodCost(ent, args.BloodCost);
        if (!CheckBloodEssence(ent.Owner, bloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (_cuffable.TryGetLastCuff(ent.Owner, out var cuffs))
            _cuffable.Uncuff(ent.Owner, null, cuffs.Value);

        var hound = _polymorph.PolymorphEntity(ent, args.PolymorphProto);
        if (hound == null)
            return;

        Spawn(args.MistEffect, Transform(hound.Value).Coordinates);
        ConfiguratePolymorph(ent, hound.Value);

        var heartCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Heart);
        var lungsCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Lungs);
        var kidneysCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Kidneys);
        var liverCount = GetOrganTypeCount(ent.Owner, BestiaOrganType.Liver);

        var maxHealth = CalculateHealth(heartCount, 140f, 30f, 320f);
        var damageBonus = CalculateDamageBonus(heartCount, 0f, 1f, 6f);
        var speedMultiplier = CalculateSpeedMultiplier(lungsCount, 1f, 0.05f, 1.3f);
        var healAmount = CalculateValue(kidneysCount, 1, 1f, 10f);
        var bloodPerBite = CalculateValue(liverCount, 0, 0.5f, 10f);

        _threshold.SetMobStateThreshold(hound.Value, maxHealth, MobState.Dead);

        if (TryComp<MeleeWeaponComponent>(hound, out var weapon))
        {
            var damageDict = weapon.Damage.DamageDict;
            var damageValue = (int)Math.Round(damageBonus);

            if (damageDict.ContainsKey(args.BonusDamageType))
                damageDict[args.BonusDamageType] += damageValue;
            else
                damageDict[args.BonusDamageType] = damageValue;
        }

        if (TryComp<MovementSpeedModifierComponent>(hound, out var speed))
        {
            _speed.ChangeBaseSpeed(hound.Value,
                speed.BaseWalkSpeed * speedMultiplier,
                speed.BaseSprintSpeed * speedMultiplier,
                speed.BaseAcceleration);
        }

        if (TryComp<LeechMeleeWeaponComponent>(hound, out var leech))
        {
            if (leech.Heal != null) leech.Heal *= healAmount;
            if (leech.HealGroups != null) leech.HealGroups *= healAmount;
        }

        var absorption = EnsureComp<VampireBloodAbsorptionComponent>(hound.Value);
        absorption.VampireOwner = ent.Owner;
        absorption.BloodStealAmount = bloodPerBite;

        SubtractBloodEssence(ent.Owner, bloodCost);
        args.Handled = true;
    }

    #endregion

    #region Polymorph Block

    private void OnPolymorphed(Entity<VampirePolymorphComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        if (TryComp(args.NewEntity, out VampireComponent? vampire))
            AddActions((args.NewEntity, vampire), args.NewEntity);

        if (TryComp<NightVisionComponent>(ent, out var formVision) && TryComp<NightVisionComponent>(args.NewEntity, out var bodyVision))
            _nightVision.SetActive((args.NewEntity, bodyVision), formVision.State == NightVisionState.Full, bodyVision.Overlay);
    }

    private void OnResonantShriek(Entity<VampirePolymorphComponent> ent, ref VampireResonantShriekActionEvent args)
    {
        var vampire = ent.Comp.Body;
        if (!HasComp<VampireComponent>(vampire))
            return;

        var bloodCost = GetAdjustedBloodCost(vampire, args.BloodCost);
        if (!CheckBloodEssence(vampire, bloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        var heartCount = GetOrganTypeCount(vampire, BestiaOrganType.Heart);
        var stomachCount = GetOrganTypeCount(vampire, BestiaOrganType.Stomach);

        var range = CalculateRange(stomachCount, 2f, 0.33f, 5f);
        var stunDuration = CalculateDuration(heartCount, 0.5f, 0.33f, 2f);

        _audio.PlayPvs(args.Sound, ent);
        var nearbyEntities = _entityLookup.GetEntitiesInRange<DamageableComponent>(Transform(ent).Coordinates, range)
            .Where(entity => !HasComp<MobStateComponent>(entity.Owner)).ToList();

        foreach (var entity in nearbyEntities)
        {
            if ((!TryComp(entity.Owner, out PhysicsComponent? physics) || physics.CollisionLayer != (int)CollisionGroup.GlassLayer)
                && !HasComp<PointLightComponent>(entity.Owner))
                continue;

            _damage.TryChangeDamage(entity.Owner, args.Damage, true);
        }

        var allEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(ent).Coordinates, range);
        foreach (var entity in allEntities)
        {
            if (ent.Owner == entity.Owner)
                continue;

            _stun.TryAddStunDuration(entity.Owner, TimeSpan.FromSeconds(stunDuration));
        }

        SubtractBloodEssence(vampire, bloodCost);
        args.Handled = true;
    }

    private void OnLungeFinale(Entity<VampirePolymorphComponent> ent, ref VampireLungeFinaleActionEvent args)
    {
        var vampire = ent.Comp.Body;
        if (!HasComp<VampireComponent>(vampire))
            return;

        var bloodCost = GetAdjustedBloodCost(vampire, args.BloodCost);
        if (!CheckBloodEssence(vampire, bloodCost))
        {
            SendFailedPopup(args.Performer);
            return;
        }

        var bestiaContainer = Comp<BestiaContainerComponent>(vampire);
        var totalTrophies = bestiaContainer.OrgansContainer.ContainedEntities.Count;

        var heartCount = GetOrganTypeCount(vampire, BestiaOrganType.Heart);
        var lungCount = GetOrganTypeCount(vampire, BestiaOrganType.Lungs);
        var kidneysCount = GetOrganTypeCount(vampire, BestiaOrganType.Kidneys);
        var stomachCount = GetOrganTypeCount(vampire, BestiaOrganType.Stomach);

        var jumpCount = CalculateCountFromTrophies(totalTrophies, 1, 10, 5);

        var maxRange = CalculateRange(lungCount, 5f, 0.5f, 8f);
        var stunDuration = CalculateDuration(heartCount, 0.5f, 0.5f, float.MaxValue);
        var bloodAmount = CalculateValue(kidneysCount, 5, 5f, 50f);
        var radius = CalculateRadius(stomachCount, 0.5f, 0.5f, 1.5f);

        var victims = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(ent).Coordinates, maxRange)
            .Where(x => x.Owner != ent.Owner && !_mobState.IsDead(x.Owner) && !HasComp<VampireComponent>(x.Owner))
            .Select(x => x.Owner).ToList();

        if (victims.Count == 0)
            return;

        ExecuteLungeJump(ent, victims, maxRange, stunDuration, bloodAmount, radius, jumpCount, 0);

        SubtractBloodEssence(vampire, bloodCost);
        args.Handled = true;
    }

    #endregion

    #region Utility Methods

    private bool CanDissect(EntityUid user, EntityUid target, out string? reason)
    {
        reason = null;
        if (user == target)
        {
            reason = Loc.GetString("vampire-bestia-dissect-self");
            return false;
        }

        if (!_interaction.InRangeUnobstructed(user, target))
        {
            reason = Loc.GetString("vampire-bestia-dissect-out-of-range");
            return false;
        }

        if (_mobState.IsDead(target))
        {
            reason = Loc.GetString("vampire-bestia-dissect-dead");
            return false;
        }

        if (TryComp<SSDIndicatorComponent>(target, out var targetSSD))
        {
            if (targetSSD.IsSSD && !_mobState.IsDead(target))
            {
                reason = Loc.GetString("vampire-bestia-dissect-ssd");
                return false;
            }
        }

        if (!HasComp<BodyComponent>(target) || HasComp<DnaModifiedComponent>(target))
        {
            reason = Loc.GetString("vampire-bestia-dissect-no-organs");
            return false;
        }

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) || bloodstream.BloodSolution == null)
        {
            reason = Loc.GetString("vampire-bestia-dissect-no-blood");
            return false;
        }

        var bloodSolution = bloodstream.BloodSolution.Value.Comp.Solution;
        if (bloodSolution.Contents.Count == 0)
        {
            reason = Loc.GetString("vampire-bestia-dissect-no-blood");
            return false;
        }

        var bloodReagent = bloodSolution.Contents[0].Reagent.Prototype;
        if (!BloodProto.Any(b => b.Id == bloodReagent))
        {
            reason = Loc.GetString("vampire-bestia-dissect-inappropriate");
            return false;
        }

        return true;
    }

    private bool CanDissectOrgan(EntityUid user, EntityUid target, EntityUid targetOrgan, out string? reason)
    {
        reason = null;
        if (!TryComp<BestiaContainerComponent>(user, out var bestia))
            return false;

        if (bestia.OrgansExtractedFromVictim.TryGetValue(target, out var extracted)
            && extracted >= bestia.MaxOrgansPerVictim)
        {
            reason = Loc.GetString("vampire-bestia-dissect-victim-limit-reached");
            return false;
        }

        var currentCounts = GetOrganCounts(bestia.OrgansContainer);
        var organType = GetOrganTypeEnum(targetOrgan);

        var maxCount = IsCriticalOrgan(targetOrgan) ? bestia.MaxCriticalOrgans : bestia.MaxRegularOrgans;
        if (currentCounts.TryGetValue(organType, out var count) && count >= maxCount)
        {
            reason = Loc.GetString("vampire-bestia-dissect-limit-reached", ("organ", Name(targetOrgan)));
            return false;
        }

        if (!TryComp<BodyComponent>(target, out var body) || body.Organs == null || !body.Organs.ContainedEntities.Contains(targetOrgan))
        {
            reason = Loc.GetString("vampire-bestia-dissect-extract-fail");
            return false;
        }

        return true;
    }

    private bool TryExtractOrgan(EntityUid target, EntityUid organ)
    {
        if (!TryComp<BodyComponent>(target, out var body) || body.Organs == null)
            return false;

        if (!body.Organs.ContainedEntities.Contains(organ))
            return false;

        _container.Remove(organ, body.Organs);
        return true;
    }

    private void ExecuteMarkPreyBurnTick(EntityUid vampire, EntityUid target, int currentTick, int maxTicks,
        float burnChance, float burnCount, ProtoId<DamageTypePrototype> damageType)
    {
        if (!Exists(vampire) || !Exists(target) || currentTick >= maxTicks)
            return;

        if (_random.Prob(burnChance))
        {
            var burnDamage = new DamageSpecifier() { DamageDict = { { damageType, burnCount } } };
            _damage.TryChangeDamage(target, burnDamage, true, origin: vampire);
        }

        Timer.Spawn(TimeSpan.FromSeconds(1), () => ExecuteMarkPreyBurnTick(vampire, target, currentTick + 1, maxTicks, burnChance, burnCount, damageType));
    }

    private void ConfiguratePolymorph(Entity<VampireComponent> ent, EntityUid polymorph)
    {
        EnsureComp<VampirePolymorphComponent>(polymorph).Body = ent.Owner;
        AddActions(ent, polymorph);
    }

    private void AddActions(Entity<VampireComponent> ent, EntityUid target)
    {
        // No, I don't want to reconfigure the event for the performer so that the mice or the hound can drink some blood.
        // If you want to do this, reconfigure the drinking actions to args.Performer so that it passes all checks and the blood is added to the vampire's body.
        // _action.AddActionDirect(target, ent.Comp.DrinkActionEntity);
        _action.AddActionDirect(target, ent.Comp.RejuvenateActionEntity);
        _action.AddActionDirect(target, ent.Comp.GlareActionEntity);

        foreach (var (_, actionEntity) in ent.Comp.AcquiredSkills)
        {
            if (actionEntity != null) _action.AddActionDirect(target, actionEntity.Value);
        }
    }

    private void ExecuteLungeJump(Entity<VampirePolymorphComponent> vampire, List<EntityUid> victims, float maxRange, float stunDuration, float bloodAmount, float radius, int totalJumps, int currentJump)
    {
        if (!Exists(vampire) || currentJump >= totalJumps || victims.Count == 0)
            return;

        var target = _random.Pick(victims);

        var vampirePos = _transform.GetWorldPosition(vampire);
        var targetPos = _transform.GetWorldPosition(target);
        var rawDirection = targetPos - vampirePos;
        var rawDistance = rawDirection.Length();

        var actualDistance = Math.Min(maxRange, rawDistance);
        var direction = rawDirection.Normalized() * actualDistance;

        var throwSpeed = Math.Max(5f, actualDistance / 0.5f);
        _throwing.TryThrow(vampire, direction, throwSpeed, compensateFriction: true);

        var flyTime = actualDistance / throwSpeed;
        Timer.Spawn(TimeSpan.FromSeconds(flyTime), () =>
        {
            if (!Exists(vampire))
                return;

            foreach (var humanoid in _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(vampire).Coordinates, radius))
            {
                if (humanoid.Owner == vampire.Owner)
                    continue;

                _blood.TryBleedOut(humanoid.Owner, bloodAmount);
                AddBloodEssence(vampire.Comp.Body, bloodAmount);
                _stun.TryUpdateParalyzeDuration(humanoid.Owner, TimeSpan.FromSeconds(stunDuration));
            }

            victims = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(vampire).Coordinates, maxRange)
                .Where(x => x.Owner != vampire.Owner && !_mobState.IsDead(x.Owner) && !HasComp<VampireComponent>(x.Owner))
                .Select(x => x.Owner).ToList();

            if (victims.Count == 0)
                return;

            Timer.Spawn(TimeSpan.FromSeconds(1f), () =>
                ExecuteLungeJump(vampire, victims, maxRange, stunDuration, bloodAmount, radius, totalJumps, currentJump + 1));
        });
    }

    #region Organ-based Calculations

    private float CalculateValue(float baseValue, int count, float increment, float maxValue)
    {
        return Math.Min(maxValue, baseValue + count * increment);
    }

    private float CalculateHealth(int count, float baseHealth, float healthPerOrgan, float maxHealth)
        => CalculateValue(baseHealth, count, healthPerOrgan, maxHealth);

    private float CalculateDamageBonus(int count, float baseBonus, float bonusPerOrgan, float maxBonus)
        => CalculateValue(baseBonus, count, bonusPerOrgan, maxBonus);

    private float CalculateSpeedMultiplier(int count, float baseMult, float incrementPerOrgan, float maxMult)
        => Math.Min(maxMult, baseMult + count * incrementPerOrgan);

    private float CalculateRange(int count, float baseRange, float incrementPerOrgan, float maxRange)
        => CalculateValue(baseRange, count, incrementPerOrgan, maxRange);

    private float CalculateDuration(int count, float baseDuration, float incrementPerOrgan, float maxDuration)
        => CalculateValue(baseDuration, count, incrementPerOrgan, maxDuration);

    private float CalculateRadius(int count, float baseRadius, float incrementPerOrgan, float maxRadius)
        => CalculateValue(baseRadius, count, incrementPerOrgan, maxRadius);

    private int CalculateCountFromTrophies(int trophyCount, int baseCount, int trophyStep, int maxCount)
        => Math.Clamp(baseCount + (trophyCount / trophyStep), 1, maxCount);

    private float CalculateChance(int count, float baseChance, float chancePerOrgan, float maxChance)
        => Math.Min(maxChance, baseChance + count * chancePerOrgan);

    private float CalculateBonusMultiplier(int organCount, int maxValue, float percentPerOrgan = 5f)
    {
        var bonus = Math.Clamp(organCount * percentPerOrgan, 0, maxValue * percentPerOrgan);
        return 1f - (bonus / 100f);
    }

    private FixedPoint2 GetAdjustedBloodCost(EntityUid uid, FixedPoint2 baseCost)
    {
        var liverCount = GetOrganTypeCount(uid, BestiaOrganType.Liver);
        var reduction = liverCount * 2;

        var reductionPercent = Math.Min(reduction, 50);
        var multiplier = 1f - (reductionPercent / 100f);

        return baseCost * multiplier;
    }

    #endregion Organ-based Calculations

    #endregion Utility Methods
}
