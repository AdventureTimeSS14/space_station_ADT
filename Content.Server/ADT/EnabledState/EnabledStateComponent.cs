namespace Content.Server.ADT.Fun;

[RegisterComponent]
public sealed partial class EnabledStateComponent : Component
{
    [DataField]
    public bool Enabled = false;
}

public sealed class EnabledStateChangedEvent : EntityEventArgs
{
    public bool OldState { get; }
    public bool NewState { get; }

    public EnabledStateChangedEvent(bool oldState, bool newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}
