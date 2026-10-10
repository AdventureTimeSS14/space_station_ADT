using System.Collections.Generic;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Shields;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Shields;

/// <summary>Per-tile shield: урон, коллапс, регенерация и диффузия сегментов.</summary>
public sealed partial class ShieldSegmentSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ShieldDiffusionSystem _diffusion = default!;
    [Dependency] private readonly ShieldGridSystem _grid = default!;

    private const string ImpactSound = "/Audio/Weapons/genhit1.ogg";
    private const string FixtureId = "fix1";

    private const float CriticalHitChanceBase = 10f;
    private const float MajorHitChanceBase = 20f;
    private const float MinorHitChanceBase = 35f;

    private const float ImpactVolumeMin = -10f;
    private const float ImpactVolumeMax = 10f;
    private const float ImpactVolumePerDamage = 0.4f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShieldSegmentComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<ShieldSegmentComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ShieldSegmentComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ShieldSegmentComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnBeforeDamage(EntityUid uid, ShieldSegmentComponent seg, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (seg.Generator is not { } genUid || !TryComp<ShieldGeneratorComponent>(genUid, out var gen))
        {
            QueueDel(uid);
            args.Cancelled = true;
            return;
        }

        var damage = SharedShieldSystem.GetTotalDamage(args.Damage);
        if (damage > 0)
        {
            var damType = SharedShieldSystem.GetShieldDamType(args.Damage);
            TakeDamage(uid, seg, genUid, gen, damage, damType, args.Origin);
        }

        args.Cancelled = true;
    }

    private void OnCollide(EntityUid uid, ShieldSegmentComponent seg, ref StartCollideEvent args)
    {
        if (seg.Generator is not { } genUid || !TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            return;

        if (!gen.Tiles.TryGetValue(seg.GridPos, out var data) || !IsActive(data))
            return;

        var other = args.OtherEntity;
        if (!gen.Modes.HasMode(ShieldModes.Overcharge))
            return;

        if (!TryComp<DamageableComponent>(other, out _) || !HasComp<MobStateComponent>(other))
            return;

        var shock = new DamageSpecifier(_proto.Index<DamageTypePrototype>(SharedShieldSystem.HeatTypeId), gen.OverchargeShockDamage);
        _damageable.TryChangeDamage(other, shock, origin: uid);
        TakeDamage(uid, seg, genUid, gen, gen.OverchargeFieldStrain, ShieldDamType.Em, uid);
    }

    private void OnPreventCollide(EntityUid uid, ShieldSegmentComponent seg, ref PreventCollideEvent args)
    {
        if (seg.Generator is not { } genUid || !TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            return;

        var humanoidsOn = gen.Modes.HasMode(ShieldModes.Humanoids);
        var anorganicOn = gen.Modes.HasMode(ShieldModes.Anorganic);
        if (!humanoidsOn && !anorganicOn)
            return;

        if (!TryComp<InjurableComponent>(args.OtherEntity, out var injurable)
            || injurable.DamageContainer is not { } container)
            return;

        var isHumanoid = gen.HumanoidContainers.Contains(container);
        var isAnorganic = gen.AnorganicContainers.Contains(container);

        if ((isHumanoid && !humanoidsOn) || (isAnorganic && !anorganicOn))
            args.Cancelled = true;
    }

    private void OnShutdown(EntityUid uid, ShieldSegmentComponent seg, ComponentShutdown args)
    {
        if (seg.Generator is { } genUid && TryComp<ShieldGeneratorComponent>(genUid, out var gen))
        {
            gen.Segments.Remove(uid);
            if (gen.Tiles.Remove(seg.GridPos, out var data))
                gen.DamagedTiles.Remove(seg.GridPos);
        }

        UpdateAtmosphericBlocking(seg, register: false);

        if (seg.Grid != default)
        {
            var removed = new ShieldSegmentRemovedEvent(seg.Grid, seg.GridPos);
            RaiseLocalEvent(ref removed);
        }
    }

    public void Fail(EntityUid uid, ShieldSegmentComponent seg, float duration)
    {
        if (duration <= 0 || TerminatingOrDeleted(uid))
            return;

        if (seg.Generator is not { } genUid || !TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            return;

        if (!gen.Tiles.TryGetValue(seg.GridPos, out var data))
            return;

        gen.DamagedTiles.Add(seg.GridPos);
        data.DisabledFor = MathF.Max(data.DisabledFor, duration);
        UpdateSegmentVisuals(uid, seg, gen);
    }

    public void Regenerate(EntityUid uid, ShieldSegmentComponent seg, ShieldGeneratorComponent gen, float dt)
    {
        if (!gen.Tiles.TryGetValue(seg.GridPos, out var data))
            return;

        data.DisabledFor = MathF.Max(0, data.DisabledFor - dt);
        data.DiffusedFor = MathF.Max(0, data.DiffusedFor - dt);

        if (data.DisabledFor > 0 || data.DiffusedFor > 0)
            return;

        if (seg.Grid != default && _diffusion.IsDiffused(seg.Grid, seg.GridPos))
        {
            var duration = _diffusion.GetDuration(seg.Grid, seg.GridPos);
            data.DiffusedFor = duration > 0 ? duration : ShieldConstants.DiffuseRefreshBase;
            return;
        }

        gen.DamagedTiles.Remove(seg.GridPos);
        UpdateSegmentVisuals(uid, seg, gen);

        if (gen.Modes.HasMode(ShieldModes.Overcharge))
        {
            var coords = Transform(uid).Coordinates;
            var intersecting = new HashSet<EntityUid>();
            _turf.GetEntitiesInTile(coords, intersecting, LookupFlags.Uncontained);
            foreach (var ent in intersecting)
            {
                if (!HasComp<MobStateComponent>(ent))
                    continue;
                var shock = new DamageSpecifier(_proto.Index<DamageTypePrototype>(SharedShieldSystem.HeatTypeId), gen.OverchargeShockDamage);
                _damageable.TryChangeDamage(ent, shock, origin: uid);
            }
        }
    }

    public void Diffuse(EntityUid uid, ShieldSegmentComponent seg, float duration)
    {
        if (TerminatingOrDeleted(uid))
            return;

        if (seg.Generator is not { } genUid || !TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            return;

        if (!gen.Tiles.TryGetValue(seg.GridPos, out var data))
            return;

        if (gen.Modes.HasMode(ShieldModes.Bypass) && data.DiffusedFor <= 0 && data.DisabledFor <= 0)
        {
            TakeDamage(uid, seg, genUid, gen, duration * _random.Next((int) gen.BypassStrainMin, (int) gen.BypassStrainMax), ShieldDamType.Em, uid);
            return;
        }

        data.DiffusedFor = MathF.Max(data.DiffusedFor, duration);
        gen.DamagedTiles.Add(seg.GridPos);
        UpdateSegmentVisuals(uid, seg, gen);
    }

/// <summary>Применяет урон по щиту и возвращает уровень попадания.</summary>
    public ShieldHitLevel TakeDamage(EntityUid uid, ShieldSegmentComponent seg, EntityUid genUid, ShieldGeneratorComponent gen, float damage, ShieldDamType damType, EntityUid? origin)
    {
        if (damage <= 0)
            return ShieldHitLevel.Absorbed;

        var energyToUse = ApplyMitigation(gen, damType, damage * ShieldConstants.EnergyPerHp);

        var exhausted = false;
        if (TryComp<BatteryComponent>(genUid, out var battery))
        {
            var before = _battery.GetCharge((genUid, battery));
            _battery.ChangeCharge((genUid, battery), -energyToUse);
            if (before > 0 && _battery.GetCharge((genUid, battery)) <= 0)
                exhausted = true;
        }

        PlayImpact(uid, gen, damage);

        if (exhausted)
        {
            var failureEvent = new ShieldEnergyFailureEvent(genUid);
            RaiseLocalEvent(genUid, ref failureEvent);
            return ShieldHitLevel.Failure;
        }

        var integrity = FieldIntegrity(genUid, gen);
        if (integrity <= 0)
            return ShieldHitLevel.Failure;

        ShieldHitLevel level;
        if (_random.Next(100) < (CriticalHitChanceBase - integrity))
            level = ShieldHitLevel.Critical;
        else if (_random.Next(100) < (MajorHitChanceBase - integrity))
            level = ShieldHitLevel.Major;
        else if (_random.Next(100) < (MinorHitChanceBase - integrity))
            level = ShieldHitLevel.Minor;
        else
            level = ShieldHitLevel.Absorbed;

        if (level != ShieldHitLevel.Absorbed)
            FailAdjacentSegments(genUid, gen, uid, level);

        return level;
    }

    public float FieldIntegrity(EntityUid genUid, ShieldGeneratorComponent gen)
    {
        if (!TryComp<BatteryComponent>(genUid, out var battery) || battery.MaxCharge <= 0)
            return 0;

        return _battery.GetCharge((genUid, battery)) / battery.MaxCharge * 100f;
    }

    private static float ApplyMitigation(ShieldGeneratorComponent gen, ShieldDamType damType, float energyToUse)
    {
        var mitigation = damType switch
        {
            ShieldDamType.Physical => gen.MitigationPhysical,
            ShieldDamType.Em => gen.MitigationEm,
            ShieldDamType.Heat => gen.MitigationHeat,
            _ => 0f,
        };

        var result = energyToUse * (1 - mitigation / 100f);

        gen.MitigationPhysical = MathF.Max(0, gen.MitigationPhysical - ShieldConstants.MitigationHitLoss);
        gen.MitigationEm = MathF.Max(0, gen.MitigationEm - ShieldConstants.MitigationHitLoss);
        gen.MitigationHeat = MathF.Max(0, gen.MitigationHeat - ShieldConstants.MitigationHitLoss);

        if (gen.Modes.HasMode(ShieldModes.Modulate))
        {
            var gain = ShieldConstants.MitigationHitLoss + ShieldConstants.MitigationHitGain;
            switch (damType)
            {
                case ShieldDamType.Physical:
                    gen.MitigationPhysical += gain;
                    break;
                case ShieldDamType.Em:
                    gen.MitigationEm += gain;
                    break;
                case ShieldDamType.Heat:
                    gen.MitigationHeat += gain;
                    break;
            }
        }

        gen.MitigationPhysical = MathF.Min(gen.MitigationPhysical, gen.MitigationMax);
        gen.MitigationEm = MathF.Min(gen.MitigationEm, gen.MitigationMax);
        gen.MitigationHeat = MathF.Min(gen.MitigationHeat, gen.MitigationMax);

        return result;
    }

    public void FailAdjacentSegments(EntityUid genUid, ShieldGeneratorComponent gen, EntityUid hitSegment, ShieldHitLevel hitLevel)
    {
        if (!TryComp<ShieldSegmentComponent>(hitSegment, out var hitSeg))
            return;

        var range = hitLevel switch
        {
            ShieldHitLevel.Minor => _random.Next(1, 4),
            ShieldHitLevel.Major => _random.Next(2, 6),
            ShieldHitLevel.Critical => _random.Next(4, 9),
            ShieldHitLevel.Failure => _random.Next(8, 17),
            _ => 0,
        };

        foreach (var segUid in gen.Segments)
        {
            if (segUid == hitSegment || !TryComp<ShieldSegmentComponent>(segUid, out var seg))
                continue;

            var dist = MathF.Abs(hitSeg.GridPos.X - seg.GridPos.X) + MathF.Abs(hitSeg.GridPos.Y - seg.GridPos.Y);
            if (dist > range)
                continue;

            Fail(segUid, seg, MathF.Max(0, (range - dist) * 2));
        }
    }

    public void UpdateSegmentVisuals(EntityUid uid, ShieldSegmentComponent seg, ShieldGeneratorComponent gen)
    {
        if (TerminatingOrDeleted(uid))
            return;

        if (!gen.Tiles.TryGetValue(seg.GridPos, out var data))
            return;

        UpdateSegmentCollision(uid, seg, gen, data);

        if (TryComp<AppearanceComponent>(uid, out _))
        {
            var active = IsActive(data);
            _appearance.SetData(uid, ShieldSegmentVisuals.Active, active);
            var overcharged = gen.Modes.HasMode(ShieldModes.Overcharge);
            _appearance.SetData(uid, ShieldSegmentVisuals.Overcharged, overcharged);
            _appearance.SetData(uid, ShieldSegmentVisuals.Floor, data.Floor);
        }

        UpdateAtmosphericSeals(uid, seg, gen, data);
    }

    private void UpdateSegmentCollision(EntityUid uid, ShieldSegmentComponent seg, ShieldGeneratorComponent gen, ShieldTileData data)
    {
        var layer = CollisionGroup.None;
        var mask = CollisionGroup.None;

        if (gen.Modes.HasMode(ShieldModes.Humanoids)
            || gen.Modes.HasMode(ShieldModes.Anorganic))
        {
            layer |= CollisionGroup.HighImpassable | CollisionGroup.MidImpassable | CollisionGroup.LowImpassable;
        }

        if (gen.Modes.HasMode(ShieldModes.Hyperkinetic))
            layer |= CollisionGroup.BulletImpassable;

        if (gen.Modes.HasMode(ShieldModes.Photonic))
            layer |= CollisionGroup.Opaque;

        var active = IsActive(data);
        var canCollide = active && layer != CollisionGroup.None;

        if (!TryComp<FixturesComponent>(uid, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(FixtureId, out var fixture))
        {
            if (TryComp<PhysicsComponent>(uid, out var body))
                _physics.SetCanCollide(uid, false, body: body);
            return;
        }

        _physics.SetCollisionLayer(uid, FixtureId, fixture, (int) layer, manager: fixtures);
        _physics.SetCollisionMask(uid, FixtureId, fixture, (int) mask, manager: fixtures);

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetCanCollide(uid, canCollide, body: physics);
    }

    private void UpdateAtmosphericSeals(EntityUid uid, ShieldSegmentComponent seg, ShieldGeneratorComponent gen, ShieldTileData data)
    {
        var active = IsActive(data);
        var shouldSeal = active
            && seg.Grid != default
            && gen.Modes.HasMode(ShieldModes.Atmospheric);

        if (shouldSeal == data.SealsActive)
            return;

        if (shouldSeal)
            UpdateAtmosphericBlocking(seg, register: true);
        else
            UpdateAtmosphericBlocking(seg, register: false);

        data.SealsActive = shouldSeal;
    }

    private static bool IsActive(ShieldTileData data)
        => data.DisabledFor <= 0 && data.DiffusedFor <= 0;

    private void UpdateAtmosphericBlocking(ShieldSegmentComponent seg, bool register)
    {
        if (!TryComp<MapGridComponent>(seg.Grid, out var grid))
            return;

        foreach (var dir in ShieldGridSystem.CardinalVectors)
        {
            var hullTile = seg.GridPos + dir;

            if (_grid.IsSpace(seg.Grid, grid, hullTile))
                continue;

            var opposite = OppositeCardinal(dir);
            var changed = register
                ? ShieldAirtightRegistry.Add(seg.Grid, hullTile, opposite)
                : ShieldAirtightRegistry.Remove(seg.Grid, hullTile, opposite);

            if (changed)
                _atmosphere.InvalidateTile(seg.Grid, hullTile);
        }
    }

    private static AtmosDirection OppositeCardinal(Vector2i dir)
    {
        var opposite = -dir;
        if (opposite == new Vector2i(0, 1))
            return AtmosDirection.North;
        if (opposite == new Vector2i(0, -1))
            return AtmosDirection.South;
        if (opposite == new Vector2i(1, 0))
            return AtmosDirection.East;
        return AtmosDirection.West;
    }

    private void PlayImpact(EntityUid uid, ShieldGeneratorComponent gen, float damage)
    {
        var coords = Transform(uid).Coordinates;

        if (!string.IsNullOrEmpty(gen.ImpactProto.Id))
            Spawn(gen.ImpactProto, coords);

        var volume = MathHelper.Clamp(ImpactVolumeMin + damage * ImpactVolumePerDamage, ImpactVolumeMin, ImpactVolumeMax);
        _audio.PlayPvs(ImpactSound, coords, AudioParams.Default.WithVolume(volume));
    }
}