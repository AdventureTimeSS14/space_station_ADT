namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class ChemMasterBufferComponent : Component
{
    [DataField]
    public float BufferCapacity = 1500f;

    [DataField]
    public float BufferMultiplier = 1f;
}
