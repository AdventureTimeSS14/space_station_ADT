namespace Content.Shared.ADT.SpasezhnikovImplant;

[RegisterComponent]
public sealed partial class SpasezhnikovImplantComponent : Component
{
    [DataField]
    public float Duration = 4f;

    [DataField]
    public float HpThresholdBeforeCrit = 20f;

    [DataField]
    public float MovementSpeedModifier = 1.8f;

    [DataField]
    public float AttackSpeedModifier = 1.8f;
}
