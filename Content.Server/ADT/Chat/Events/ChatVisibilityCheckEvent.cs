namespace Content.Server.ADT.Chat;

[ByRefEvent]
public struct ChatVisibilityCheckEvent(EntityUid source, EntityUid? target, float range, bool ignoreWalls = false)
{
    [DataField]
    public EntityUid Source { get; } = source;

    [DataField]
    public EntityUid? Target { get; } = target;

    [DataField]
    public float Range { get; } = range;

    [DataField]
    public bool IgnoreWalls { get; } = ignoreWalls;

    [DataField]
    public bool Visible { get; set; } = true;
}

