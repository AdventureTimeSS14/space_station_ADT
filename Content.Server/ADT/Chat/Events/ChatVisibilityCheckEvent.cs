namespace Content.Server.ADT.Chat;

[ByRefEvent]
public struct ChatVisibilityCheckEvent(EntityUid source, EntityUid? target, float range, bool ignoreWalls = false)
{
    public EntityUid Source { get; } = source;

    public EntityUid? Target { get; } = target;

    public float Range { get; } = range;

    public bool IgnoreWalls { get; } = ignoreWalls;

    public bool Visible { get; set; } = true;
}

