using System.Linq;
using System.Numerics;
using Content.Server.ADT.Weapons.Hitscan.Components;
using Content.Server.Decals;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Decals;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Weapons.Hitscan.Systems;

/// <summary>
/// Starlight Shooting 2.0: leaves a blood-splatter decal at the hit point.
/// </summary>
public sealed partial class HitscanCreateBloodSpraySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    private readonly Dictionary<string, string[]> _bloodDecals = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanCreateBloodSprayComponent, HitscanDamageDealtEvent>(OnHitscanHit);
    }

    private string[] DecalsFor(string tag)
    {
        if (_bloodDecals.TryGetValue(tag, out var cached))
            return cached;

        var found = _proto.EnumeratePrototypes<DecalPrototype>()
            .Where(x => x.Tags.Contains(tag))
            .Select(x => x.ID)
            .ToArray();
        _bloodDecals[tag] = found;
        return found;
    }

    private void OnHitscanHit(Entity<HitscanCreateBloodSprayComponent> ent, ref HitscanDamageDealtEvent args)
    {
        var decals = DecalsFor(ent.Comp.DecalTag);
        if (decals.Length == 0)
            return;

        // No HP damage → no blood (also skips armor-zeroed hits).
        if (args.DamageDealt.GetTotal() <= 0)
            return;

        if (!HasComp<BloodstreamComponent>(args.Target))
            return;

        var data = args.Data;
        var gridUid = Transform(data.Gun).GridUid;
        if (gridUid is not { } grid || !TryComp(grid, out TransformComponent? gridXform))
            return;

        // Shot direction in grid space. ArtAngle is how the decal art is painted;
        // subtracting it points that streak along the bullet, into the target.
        var (_, gridRot, gridInv) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
        var shotAngle = data.ShotDirection.ToAngle() - gridRot;
        var mapPos = data.HitPosition ?? _transform.GetWorldPosition(args.Target);
        var gridPos = Vector2.Transform(mapPos, gridInv) - shotAngle.ToVec() * ent.Comp.Offset;
        var coords = new EntityCoordinates(grid, gridPos);
        var rotation = shotAngle - ent.Comp.ArtAngle;
        var color = _bloodstream.GetBloodColor(args.Target);

        var target = args.Target;
        Timer.Spawn((int) ent.Comp.Delay.TotalMilliseconds, () =>
        {
            if (Deleted(target) || decals.Length == 0)
                return;

            _decal.TryAddDecal(
                _random.Pick(decals),
                coords,
                out _,
                color,
                rotation,
                cleanable: true);
        });
    }
}
