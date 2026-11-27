using System.Numerics;
using Content.Shared.ADT.Combat.Ranged.Pierce;
using Content.Shared.ADT.Weapons.Hitscan.Components;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Shared.ADT.Weapons.Hitscan.Systems;

/// <summary>
/// After a successful hit, may continue the hitscan trace through the target.
/// </summary>
public sealed class HitscanPierceSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<HitscanReflectComponent> _reflectQuery;
    private static readonly ProtoId<TagPrototype> ShieldTag = "Shield";
    private const int PierceSalt = -7;

    public static int? GetNextSeed(int? seed, int depth)
        => seed is { } value ? SharedRandomExtensions.HashCodeCombine(value, depth, PierceSalt) : null;

    public bool TryPierce(Entity<HitscanPierceComponent> hitscan, EntityUid hitEntity, Vector2 shotDirection, int? seed, out Vector2 direction)
    {
        direction = default;

        if (hitscan.Comp.Chance <= 0)
            return false;

        var random = seed is { } value ? new System.Random(SharedRandomExtensions.HashCodeCombine(value, PierceSalt)) : null;

        if (hitscan.Comp.Chance < 1 && !(random?.Prob(hitscan.Comp.Chance) ?? _rand.Prob(hitscan.Comp.Chance)))
            return false;

        if (!_reflectQuery.TryComp(hitscan.Owner, out var reflect) || reflect.CurrentReflections > reflect.MaxReflections)
            return false;

        var ev = new HitScanPierceAttemptEvent(hitscan.Comp.PierceLevel, true);
        RaiseLocalEvent(hitEntity, ref ev);

        if (ev.Pierced)
        {
            foreach (var held in _handsSystem.EnumerateHeld(hitEntity))
            {
                if (!_tag.HasTag(held, ShieldTag)
                    || !TryComp<PierceableComponent>(held, out var pierceable)
                    || pierceable.Level <= hitscan.Comp.PierceLevel
                    || (TryComp<ItemToggleComponent>(held, out var itemToggle) && !itemToggle.Activated))
                    continue;

                ev.Pierced = false;
                break;
            }
        }

        if (!ev.Pierced)
            return false;

        var swim = random != null
            ? (float) ((random.NextDouble() * 2) - 1) * hitscan.Comp.Deviation
            : _rand.NextFloat(-hitscan.Comp.Deviation, hitscan.Comp.Deviation);

        direction = (shotDirection.ToAngle() + swim).ToVec();
        return true;
    }

    public override void Initialize()
    {
        base.Initialize();

        _reflectQuery = GetEntityQuery<HitscanReflectComponent>();

        SubscribeLocalEvent<HitscanPierceComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
        SubscribeLocalEvent<PierceableComponent, HitScanPierceAttemptEvent>(OnPierceablePierce);
        SubscribeLocalEvent<PierceableComponent, InventoryRelayedEvent<HitScanPierceAttemptEvent>>(OnArmorPierce);
    }

    private void OnHitscanHit(Entity<HitscanPierceComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        var data = args.Data;

        if (data.HitEntity == null
            || !TryPierce(hitscan, data.HitEntity.Value, data.ShotDirection, data.PredictionSeed, out var dir)
            || !_reflectQuery.TryComp(hitscan.Owner, out var reflect))
            return;

        reflect.CurrentReflections++;

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;
        if (Transform(data.HitEntity.Value).MapUid is { } hitMap && data.HitPosition is { } hitPosition)
            fromEffect = new EntityCoordinates(hitMap, hitPosition);

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ToCoordinates = fromEffect.Offset(dir),
            ShotDirection = dir,
            Gun = data.Gun,
            Shooter = data.Shooter,
            IgnoredEntity = data.HitEntity,
            OutputTrace = data.OutputTrace,
            PredictionSeed = GetNextSeed(data.PredictionSeed, reflect.CurrentReflections),
        };

        RaiseLocalEvent(hitscan, ref hitFiredEvent);
    }

    private void OnArmorPierce(Entity<PierceableComponent> ent, ref InventoryRelayedEvent<HitScanPierceAttemptEvent> args)
    {
        if ((byte) ent.Comp.Level > (byte) args.Args.Level)
            args.Args.Pierced = false;
    }

    private void OnPierceablePierce(Entity<PierceableComponent> ent, ref HitScanPierceAttemptEvent args)
    {
        if ((byte) ent.Comp.Level > (byte) args.Level)
            args.Pierced = false;
    }
}
