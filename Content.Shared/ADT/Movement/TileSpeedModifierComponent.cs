namespace Content.Shared.ADT.Movement;

[RegisterComponent]
[Access(typeof(TileSpeedModifierSystem))]
public sealed partial class TileSpeedModifierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float WalkSpeedModifier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SprintSpeedModifier = 1f;
}
