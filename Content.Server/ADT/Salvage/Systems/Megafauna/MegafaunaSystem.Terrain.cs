using Content.Shared.Physics;
using Content.Shared.Wall;
using FixturesComponent = Robust.Shared.Physics.FixturesComponent;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    private const int WallLayer = (int) CollisionGroup.WallLayer;

    public bool IsWall(EntityUid uid)
    {
        if (HasComp<WallComponent>(uid))
            return true;

        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return false;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if ((fixture.CollisionLayer & WallLayer) == WallLayer)
                return true;
        }

        return false;
    }

    public void Disintegrate(EntityUid target, string? remainsProto)
    {
        if (TerminatingOrDeleted(target))
            return;

        if (remainsProto != null)
            Spawn(remainsProto, _transform.GetMoverCoordinates(target));

        QueueDel(target);
    }
}
