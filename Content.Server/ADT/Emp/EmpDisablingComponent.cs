namespace Content.Server.ADT.EMPProtaction;

[RegisterComponent]
public sealed partial class EmpDisablingComponent : Component
{
    [DataField]
    public TimeSpan DisablingTime;
}
