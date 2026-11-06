namespace Content.Server.ADT.BarbellBench.Components;

[RegisterComponent]
public sealed partial class ActiveBarbellPinnedComponent : Component
{
}

[RegisterComponent]
public sealed partial class ActiveBarbellRepComponent : Component
{
    [ViewVariables]
    public EntityUid Performer;

    [ViewVariables]
    public TimeSpan SoundTime;

    [ViewVariables]
    public TimeSpan EndTime;

    [ViewVariables]
    public bool SoundPlayed;
}
