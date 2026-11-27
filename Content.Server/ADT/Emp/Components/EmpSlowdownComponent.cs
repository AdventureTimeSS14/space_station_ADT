namespace Content.Server.ADT.Emp.Components;

[RegisterComponent]
public sealed partial class EmpSlowdownComponent : Component
{
    [DataField]
    public float DurationMultiplier = 1f;

    [DataField]
    public float MaxDuration = 15f;
}
