namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class StasisBedMachinePartComponent : Component
{
    [DataField]
    public float PowerLoad = 1000f;

    [DataField]
    public float PowerMultiplier = 1f;
}
