namespace Content.Server.ADT.BarbellBench.Components;

[RegisterComponent]
public sealed partial class ActiveBarbellPinnedComponent : Component
{
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveBarbellRepComponent : Component
{
    [ViewVariables]
    public EntityUid Performer;

    [ViewVariables, AutoPausedField]
    public TimeSpan SoundTime;

    [ViewVariables, AutoPausedField]
    public TimeSpan EndTime;

    [ViewVariables]
    public bool SoundPlayed;
}
