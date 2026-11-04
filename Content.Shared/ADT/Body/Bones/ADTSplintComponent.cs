using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Body.Bones;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTSplintComponent : Component
{
    [DataField]
    public TimeSpan SelfDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan OtherDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public float Durability = 2000f;
}
