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

        if (hitscan.Comp.Chance <= 0 || data.HitEntity == null)
            return;

        if (hitscan.Comp.Chance < 1 && !_rand.Prob(hitscan.Comp.Chance))
            return;

        if (!_reflectQuery.TryComp(hitscan.Owner, out var reflect) || reflect.CurrentReflections > reflect.MaxReflections)
            return;

        var ev = new HitScanPierceAttemptEvent(hitscan.Comp.PierceLevel, true);
        RaiseLocalEvent(data.HitEntity.Value, ref ev);

        if (ev.Pierced)
        {
            foreach (var held in _handsSystem.EnumerateHeld(data.HitEntity.Value))
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
            return;

        reflect.CurrentReflections++;

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;
        if (Transform(data.HitEntity.Value).MapUid is { } hitMap && data.HitPosition is { } hitPosition)
            fromEffect = new EntityCoordinates(hitMap, hitPosition);

        var random = _rand.NextFloat(-hitscan.Comp.Deviation, hitscan.Comp.Deviation);
        var dir = (data.ShotDirection.ToAngle() + random).ToVec();

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ToCoordinates = fromEffect.Offset(dir), // ADT hitscan #3142
            ShotDirection = dir,
            Gun = data.Gun,
            Shooter = data.Shooter, // keep original shooter ignored
            IgnoredEntity = data.HitEntity, // don't immediately re-hit pierced body
            OutputTrace = data.OutputTrace,
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
