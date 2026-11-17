// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.Damage.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Physics;
using Robust.Server.Containers;
using Content.Shared.Inventory;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Shadow;

namespace Content.Server.ADT.Shadow;

public sealed partial class PhotophobiaSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private HandsSystem _handsSystem = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ProtoId<DamageTypePrototype> Damage = "Heat";

    // private readonly List<EntityUid> _scratchLights = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotophobiaComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<PhotophobiaComponent, DamageDealtEvent>(OnDamageChanged);
        SubscribeLocalEvent<PhotophobiaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotophobiaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime < comp.NextTickTime)
                continue;

            comp.NextTickTime = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.Interval * _random.NextFloat(0.75f, 1.25f));

            var lights = CheckNearbyLights(uid);

            if (!lights)
            {
                if (comp.ApplyShadowWeakness)
                {
                    _speed.RefreshMovementSpeedModifiers(uid);
                    comp.ShadowWeakness = false;
                }
                continue;
            }

            if (comp.ApplyShadowWeakness)
            {
                _speed.RefreshMovementSpeedModifiers(uid);
                comp.ShadowWeakness = true;
            }

            if (comp.DamagePerLight != 0)
            {
                var damage = new DamageSpecifier { DamageDict = { { Damage, comp.DamagePerLight } } };
                _audio.PlayPvs(comp.DamageSound, uid, AudioParams.Default.WithVolume(-10f).WithPitchScale(1.15f).WithMaxDistance(2f));
                _damageable.TryChangeDamage(uid, damage, false, false);
            }

        }
    }

    private void OnUnpaused(EntityUid uid, PhotophobiaComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextTickTime += args.PausedTime;
    }

    private bool CheckNearbyLights(EntityUid uid)
    {
        var xform = Transform(uid);
        if (HasComp<ADTLavalandMapComponent>(xform.MapUid))
            return true;

        var worldPos = _transform.GetWorldPosition(xform);
        foreach (var (lightUid, light) in _lookup.GetEntitiesInRange<PointLightComponent>(xform.Coordinates, 10f))
        {
            if (!light.Enabled)
                continue;

            if (light.Radius < 2f)
                continue;

            if (TryComp<VisibilityComponent>(lightUid, out var visibility) && visibility.Layer != 1)
                continue;

            if (TryCheckContainingLight(lightUid))
            {
                continue;
            }

            if (IsTargetInsideCone(uid, lightUid))
            {
                continue;
            }


            float lightFactor = light.Energy * 0.5f + light.Radius - 2f;

            if (lightFactor > light.Radius)
                lightFactor = light.Radius - 0.5f;

            var lightXform = Transform(lightUid);
            var lightPos = _transform.GetWorldPosition(lightXform);
            var distanceSq = (worldPos - lightPos).LengthSquared();

            if (distanceSq > lightFactor * lightFactor)
            {
                continue;
            }

            if (GetOccluded(uid, lightUid))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryCheckContainingLight(EntityUid uid)
    {
        if (!_container.TryGetContainingContainer(uid, out var container))
            return false;

        if (_inventorySystem.TryGetContainingSlot(uid, out _))
            return false;

        if (TryCheckHandLight(container.Owner, uid))
            return false;

        return true;
    }

    private bool TryCheckHandLight(Entity<HandsComponent?> user, EntityUid uid)
    {
        if (Resolve(user.Owner, ref user.Comp, false))
        {
            foreach (var held in _handsSystem.EnumerateHeld(user))
            {
                if (held == uid)
                    return true;
            }
        }

        return false;
    }

    public bool GetOccluded(EntityUid sourceUid, EntityUid lightUid)
    {
        if (sourceUid == lightUid)
            return true;

        if (_container.TryGetContainingContainer(lightUid, out var container) && container.Owner == sourceUid)
            return true;

        var sourceXform = Transform(sourceUid);
        var lightXform = Transform(lightUid);

        if (sourceXform.MapID != lightXform.MapID)
            return false;

        var sourcePos = _transform.GetWorldPosition(sourceXform);
        var lightPos = _transform.GetWorldPosition(lightXform);
        var direction = lightPos - sourcePos;
        var distance = direction.Length();

        if (distance <= 0.1f)
            return false;

        var normalizedDir = direction.Normalized();
        var ray = new CollisionRay(sourcePos, normalizedDir, (int)(CollisionGroup.WallLayer | CollisionGroup.AirlockLayer));
        var rayCastResults = _physics.IntersectRay(sourceXform.MapID, ray, distance, sourceUid, false);

        foreach (var result in rayCastResults)
        {
            if (TryComp<OccluderComponent>(result.HitEntity, out var occluder) && occluder.Enabled)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsTargetInsideCone(EntityUid targetUid, EntityUid lightUid)
    {
        if (!HasComp<PointLightComponent>(lightUid))
            return true;

        var tXform = Transform(targetUid);
        var lXform = Transform(lightUid);
        var tPos = _transform.GetWorldPosition(tXform);
        var lPos = _transform.GetWorldPosition(lXform);

        float toX = tPos.X - lPos.X;
        float toY = tPos.Y - lPos.Y;

        float dist = MathF.Sqrt(toX * toX + toY * toY);

        float invDist = 1f / dist;
        float toNx = toX * invDist;
        float toNy = toY * invDist;

        float ang = (float)_transform.GetWorldRotation(lXform).Theta;

        ang -= MathF.PI / 2f;

        float fX = MathF.Cos(ang);
        float fY = MathF.Sin(ang);

        float dot = fX * toNx + fY * toNy;

        const float coneAngleDeg = 90f;
        float cosHalf = MathF.Cos(MathF.PI * coneAngleDeg / 360f);

        return dot <= cosHalf;
    }

    private void OnRefreshMovementSpeedModifiers(Entity<PhotophobiaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.ShadowWeakness)
            args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    private void OnDamageChanged(Entity<PhotophobiaComponent> ent, ref DamageDealtEvent args)
    {
        if (!ent.Comp.ShadowWeakness)
            return;

        if (args.Damage.GetTotal() <= 0)
            return;

        var bonusDamage = args.Damage * ent.Comp.DamageModfier;
        _damageable.TryChangeDamage(ent.Owner, bonusDamage, true);
    }
}
