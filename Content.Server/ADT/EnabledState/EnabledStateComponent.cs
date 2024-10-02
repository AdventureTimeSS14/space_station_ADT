namespace Content.Server.ADT.Fun;

[RegisterComponent]
public sealed partial class EnabledStateComponent : Component
{
    [DataField]
    public bool Enabled = false;
}
