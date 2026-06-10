namespace Content.Shared.ADT.KereznikovImplant;

[RegisterComponent]
public sealed partial class KereznikovImplantComponent : Component
{
    [DataField]
    public float Duration = 2f;

    [DataField]
    public float MovementSpeedModifier = 1.8f;

    [DataField]
    public float AttackSpeedModifier = 1.8f;
}
