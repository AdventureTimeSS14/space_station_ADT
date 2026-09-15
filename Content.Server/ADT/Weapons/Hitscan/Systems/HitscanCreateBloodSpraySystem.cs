using System.Linq;
using System.Numerics;
using Content.Server.ADT.Weapons.Hitscan.Components;
using Content.Server.Decals;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Decals;
using Content.Shared.Weapons.Hitscan.Events;
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
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private string[] _bloodDecals = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanCreateBloodSprayComponent, HitscanDamageDealtEvent>(OnHitscanHit);
        CacheDecals();
    }

    private void CacheDecals()
    {
        _bloodDecals = _proto.EnumeratePrototypes<DecalPrototype>()
            .Where(x => x.Tags.Contains("BloodSplatter"))
            .Select(x => x.ID)
            .ToArray();
    }

    private void OnHitscanHit(Entity<HitscanCreateBloodSprayComponent> ent, ref HitscanDamageDealtEvent args)
    {
        if (_bloodDecals.Length == 0)
            return;

        // No HP damage → no blood (also skips armor-zeroed hits).
        if (args.DamageDealt.GetTotal() <= 0)
            return;

        if (!HasComp<BloodstreamComponent>(args.Target))
            return;

        var data = args.Data;
        var gunXform = Transform(data.Gun);

        // Match HitscanBasicRaycastSystem grid-angle handling.
        var shotAngle = data.ShotDirection.ToAngle();
        if (TryComp(gunXform.GridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, _) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            shotAngle -= gridRot;
        }
        else
        {
            // Off-grid shots: skip (same limitation as Starlight).
            return;
        }

        var hitXform = Transform(args.Target);
        var gunCoords = Transform(data.Gun).Coordinates;
        var distance = Math.Abs((hitXform.Coordinates.Position - gunCoords.Position).Length());
        var color = _bloodstream.GetBloodColor(args.Target);
        var coords = hitXform.Coordinates.Offset(
            shotAngle.ToVec() * (distance / 5000f + 1.3f) + new Vector2(-0.5f, -0.5f));

        var target = args.Target;
        Timer.Spawn(200, () =>
        {
            if (Deleted(target) || _bloodDecals.Length == 0)
                return;

            _decal.TryAddDecal(
                _random.Pick(_bloodDecals),
                coords,
                out _,
                color,
                shotAngle + Angle.FromDegrees(-45),
                cleanable: true);
        });
    }
}
