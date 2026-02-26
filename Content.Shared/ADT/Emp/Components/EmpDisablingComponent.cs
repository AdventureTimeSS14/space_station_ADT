namespace Content.Shared.ADT.EMP;

[RegisterComponent]
public sealed partial class EmpDisablingComponent : Component
{
    [DataField]
    public TimeSpan DisablingTime;
}
