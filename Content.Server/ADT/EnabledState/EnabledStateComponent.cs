namespace Content.Server.ADT.EnabledState;

[RegisterComponent]
public sealed partial class EnabledStateComponent : Component
{
    [DataField]
    public bool Enabled = false;
}
