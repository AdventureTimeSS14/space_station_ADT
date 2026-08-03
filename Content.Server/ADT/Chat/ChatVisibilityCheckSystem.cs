using Content.Server.Examine;

namespace Content.Server.ADT.Chat;
public sealed class ChatVisibilityCheckSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChatVisibilityCheckEvent>(HandleVisibilityCheck);
    }

    private void HandleVisibilityCheck(ref ChatVisibilityCheckEvent ev)
    {
        if (ev.Target is null)
        {
            ev.Visible = false;
            return;
        }

        if (ev.Source == ev.Target.Value)
            return;

        if (ev.IgnoreWalls)
        {
            ev.Visible = InRange(ev.Source, ev.Target.Value, ev.Range);
            return;
        }

        if (!_examineSystem.InRangeUnOccluded(ev.Source, ev.Target.Value, ev.Range))
        {
            ev.Visible = false;
            return;
        }
    }

    private bool InRange(EntityUid origin, EntityUid other, float range)
    {
        if (Deleted(origin) || Deleted(other))
            return false;

        var xformOrigin = Transform(origin);
        var xformOther = Transform(other);

        if (xformOrigin.MapID != xformOther.MapID)
            return false;

        return xformOrigin.Coordinates.TryDistance(EntityManager, xformOther.Coordinates, out var distance)
               && distance <= range;
    }
}

