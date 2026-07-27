using Content.Shared.ADT.Mech.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Mech;

public sealed class MechThrusterVisualsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<MechThrusterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Active)
                continue;

            if (_transform.InRange(xform.Coordinates, comp.EffectLastCoordinates, comp.EffectMaxDistance) &&
                _timing.CurTime < comp.EffectTargetTime)
            {
                continue;
            }

            comp.EffectLastCoordinates = _transform.GetMoverCoordinates(xform.Coordinates);
            comp.EffectTargetTime = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectCooldown);

            CreateParticles(uid, comp, xform);
        }
    }

    private void CreateParticles(EntityUid uid, MechThrusterComponent comp, TransformComponent xform)
    {
        if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() < 1f)
            return;

        var coordinates = xform.Coordinates;
        var gridUid = _transform.GetGrid(coordinates);

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(gridUid.Value,
                _mapSystem.WorldToLocal(gridUid.Value, grid, _transform.ToMapCoordinates(coordinates).Position));
        }
        else if (xform.MapUid != null)
        {
            coordinates = new EntityCoordinates(xform.MapUid.Value, _transform.GetWorldPosition(xform));
        }
        else
        {
            return;
        }

        Spawn(comp.Effect, coordinates);
    }
}
