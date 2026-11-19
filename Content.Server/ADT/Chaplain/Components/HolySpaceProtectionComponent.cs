namespace Content.Server.ADT.Chaplain.Components;

[RegisterComponent]
public sealed partial class HolySpaceProtectionComponent : Component
{
    [DataField]
    public float HeatCoefficient = 1f;
}
