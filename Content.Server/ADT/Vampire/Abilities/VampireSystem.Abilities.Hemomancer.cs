// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using System.Linq;
using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Localizations;
using Content.Shared.Mobs.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Damage;
using Content.Shared.Standing;
using Content.Shared.Surgery.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private LoadoutSystem _loadout = default!;

    private void InitializeHemomancer()
    {
        SubscribeLocalEvent<VampireClawsComponent, MeleeHitEvent>(OnClawsHit);
        SubscribeLocalEvent<VampireClawsComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);

        SubscribeLocalEvent<VampireComponent, VampireClawsActionEvent>(GiveVampireClaws);
        SubscribeLocalEvent<VampireComponent, VampireBloodTentacleAction>(OnBloodTendrils);
        SubscribeLocalEvent<VampireComponent, VampireBloodBarrierActionEvent>(OnBloodBarrierAction);
        SubscribeLocalEvent<VampireComponent, VampireSanguinePoolActionEvent>(OnSanguinePoolAction);
        SubscribeLocalEvent<VampireComponent, VampirePredatorSensesActionEvent>(OnVampirePredatorSensesAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodEruptionActionEvent>(OnVampireBloodEruptionAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodBringersRiteActionEvent>(OnBloodBringersRite);
    }

    private void OnClawsHit(Entity<VampireClawsComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (HasComp<SyntheticOperatedComponent>(hitEnt))
                continue;

            if (!HasComp<BloodstreamComponent>(hitEnt))
                continue;

            _damage.TryChangeDamage(args.User, ent.Comp.Damage, true, false, origin: ent);
            _stamina.TakeStaminaDamage(ent, ent.Comp.StaminaMod, visual: false);
            _blood.TryModifyBloodLevel(args.User, ent.Comp.ModifyBloodLevel);
            _blood.TryModifyBleedAmount(args.User, ent.Comp.BloodlossModifier);
			
			if (!_mobState.IsDead(hitEnt))
			{
				AddBloodEssence(args.User, ent.Comp.BloodStealAmount);
			}
        }
    }

    private void OnBeforeDamageChanged(Entity<VampireClawsComponent> ent, ref BeforeDamageChangedEvent args)
    {
        var vampire = Transform(ent).ParentUid; // We assume that the current parent is a vampire.
        var supreme = GetTruePower(vampire);
        if (supreme == null)
            return;

        if (supreme.Active) args.Cancelled = true;
    }

    private void GiveVampireClaws(Entity<VampireComponent> ent, ref VampireClawsActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(ent, ref dropEvent);

        List<ProtoId<StartingGearPrototype>> gear = new() { args.ProtoId };
        _loadout.Equip(ent, gear, null);

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodTendrils(Entity<VampireComponent> ent, ref VampireBloodTentacleAction args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var coords = args.Target;
        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        for (var i = 0; i < args.ExtraSpawns; i++)
        {
            var dir = _random.PickAndTake(dirs);
            var vector = DirectionToVector2(dir);
            spawnPos.Add(coords.Offset(vector));
        }

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef)
                || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                continue;

            Spawn(args.EntityId, pos);
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodBarrierAction(Entity<VampireComponent> ent, ref VampireBloodBarrierActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var targetCoords = args.Target;
        var transform = Transform(ent);
        var direction = transform.LocalRotation.ToWorldVec().Normalized();

        var perpendicularDirection = new Vector2(-direction.Y, direction.X);

        var objectCount = 0;
        for (int i = -1; i <= 1 && objectCount < 3; i++)
        {
            var spawnPosition = targetCoords.Offset(perpendicularDirection * (1f * i));

            if (TrySpawnObjectAtPosition(spawnPosition, args.EntityId, ent))
                objectCount++;
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnSanguinePoolAction(Entity<VampireComponent> ent, ref VampireSanguinePoolActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var polymorphedEntity = _polymorph.PolymorphEntity(ent, args.PolymorphProto);
        if (polymorphedEntity == null)
            return;

        var damage = new DamageSpecifier { DamageDict = { { "Bloodloss", 10 } } };
        _blood.TryModifyBloodLevel(ent.Owner, -100.0f);
        _damage.TryChangeDamage(ent.Owner, damage);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnVampirePredatorSensesAction(Entity<VampireComponent> ent, ref VampirePredatorSensesActionEvent args)
    {
        var centerCoords = Transform(ent).Coordinates;
        var nearbyHumanoids = _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(centerCoords, 6f);

        foreach (var humanoidEntity in nearbyHumanoids)
        {
            var humanoid = humanoidEntity.Owner;
            if (humanoid == ent.Owner)
                continue;

            if (_mobState.IsIncapacitated(humanoid))
                continue;

            Spawn(args.EntityId, Transform(humanoid).Coordinates);
            _audio.PlayPvs(args.Sound, humanoid);
            _popup.PopupEntity(Loc.GetString("vampire-predator-senses-puddle"), humanoid, humanoid, PopupType.SmallCaution);
            _stun.TryUpdateParalyzeDuration(humanoid, TimeSpan.FromSeconds(4));
            args.Handled = true;
            return;
        }

        var closestHumanoid = FindClosestHumanoidOnMap(ent);
        if (closestHumanoid != null)
        {
            var direction = GetDirectionToTarget(ent, closestHumanoid.Value);
            var directionString = ContentLocalizationManager.FormatDirection(direction).ToLower();
            var msg = Loc.GetString("vampire-predator-senses-warning", ("direction", directionString));
            _popup.PopupEntity(msg, ent, ent, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("vampire-predator-senses-nobody"), ent, ent, PopupType.SmallCaution);
        }

        args.Handled = true;
    }

    private void OnVampireBloodEruptionAction(Entity<VampireComponent> ent, ref VampireBloodEruptionActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var puddlesInRange = _entityLookup.GetEntitiesInRange<PuddleComponent>(Transform(ent).Coordinates, 4f)
            .Where(puddle =>
            {
                if (TryComp<SolutionComponent>(puddle.Owner, out var solutionComp))
                {
                    return solutionComp.Solution.Contents.Any(r => BloodProto.Contains(r.Reagent.Prototype));
                }
                return false;
            })
            .ToList();

        foreach (var puddleEntity in puddlesInRange)
        {
            var entitiesOnPuddle = _entityLookup.GetEntitiesInRange<DamageableComponent>(Transform(puddleEntity.Owner).Coordinates, 0.1f)
                .Where(entity => entity.Owner != ent.Owner).ToList();

            foreach (var targetEntity in entitiesOnPuddle)
            {
                if (HasComp<NullRodOwnerComponent>(targetEntity.Owner) && !HasTruePower(ent))
                    continue;

                _damage.TryChangeDamage(targetEntity.Owner, args.Damage, origin: ent);
                _stun.TryUpdateParalyzeDuration(targetEntity.Owner, TimeSpan.FromSeconds(3));
                _popup.PopupEntity(Loc.GetString("vampire-blood-eruption-effect-message"), targetEntity.Owner, ent, PopupType.MediumCaution);
            }
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodBringersRite(Entity<VampireComponent> ent, ref VampireBloodBringersRiteActionEvent args)
    {
        var supreme = GetTruePower(ent);
        if (supreme == null)
            return;

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (supreme.Active)
        {
            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            args.Handled = true;
            return;
        }

        supreme.Active = true;
        _alerts.ShowAlert(ent.Owner, args.Alert, 1);
        Dirty(ent.Owner, supreme);

        _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-started"), ent, ent, PopupType.SmallCaution);

        ExecuteBloodBringersRiteTick(ent, supreme, args);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
    }

    #region Utility Methods

    private void ExecuteBloodBringersRiteTick(Entity<VampireComponent> ent, SupremeVampireComponent supreme, VampireBloodBringersRiteActionEvent args)
    {
        if (!Exists(ent) || !supreme.Active)
        {
            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);

            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            return;
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(ent).Coordinates, 7f)
            .Where(entity => !_mobState.IsDead(entity.Owner) && !HasComp<SyntheticOperatedComponent>(entity.Owner))
            .ToList();

        if (nearbyEntities.Count > 0)
        {
            var groupHealSpec = _damage.CreateHealFromGroups(ent.Owner, args.HealGroups);
            var scaledHealingSpec = (args.Heal + groupHealSpec) * nearbyEntities.Count;

            _damage.TryChangeDamage(ent.Owner, scaledHealingSpec, true, false, origin: ent);
            _stamina.TakeStaminaDamage(ent, args.StaminaMod * nearbyEntities.Count, visual: false);

            foreach (var entity in nearbyEntities)
            {
                if (HasComp<VampireComponent>(entity.Owner))
                    continue;

                if (HasComp<NullRodOwnerComponent>(entity.Owner) && !HasTruePower(ent))
                    continue;

                _audio.PlayPvs(args.Sound, entity);
                _blood.TryBleedOut(entity.Owner, args.BloodCost);
                _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-affected"), entity.Owner, entity.Owner, PopupType.SmallCaution);
            }
        }

        Timer.Spawn(args.TimeInterval, () => ExecuteBloodBringersRiteTick(ent, supreme, args));
    }

    private EntityUid? FindClosestHumanoidOnMap(Entity<VampireComponent> ent)
    {
        var currentMap = Transform(ent).MapID;
        var currentCoords = Transform(ent).Coordinates;

        EntityUid? closestHumanoid = null;
        float closestDistance = float.MaxValue;

        var query = EntityQueryEnumerator<HumanoidProfileComponent, TransformComponent>();
        while (query.MoveNext(out var humanoid, out _, out var transform))
        {
            if (humanoid == ent.Owner)
                continue;

            if (transform.MapID != currentMap)
                continue;

            if (_mobState.IsIncapacitated(humanoid))
                continue;

            var humanoidCoords = _transform.GetMapCoordinates(humanoid, transform);
            var distance = Vector2.Distance(currentCoords.Position, humanoidCoords.Position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHumanoid = humanoid;
            }
        }

        return closestHumanoid;
    }

    private Direction GetDirectionToTarget(Entity<VampireComponent> source, EntityUid target)
    {
        var sourceCoords = Transform(source).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        var directionVector = targetCoords.Position - sourceCoords.Position;
        return directionVector.GetDir();
    }

    private Vector2 DirectionToVector2(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector2(0, 1),
            Direction.South => new Vector2(0, -1),
            Direction.East => new Vector2(1, 0),
            Direction.West => new Vector2(-1, 0),
            Direction.NorthEast => new Vector2(1, 1).Normalized(),
            Direction.NorthWest => new Vector2(-1, 1).Normalized(),
            Direction.SouthEast => new Vector2(1, -1).Normalized(),
            Direction.SouthWest => new Vector2(-1, -1).Normalized(),
            _ => Vector2.Zero,
        };
    }

    #endregion
}
