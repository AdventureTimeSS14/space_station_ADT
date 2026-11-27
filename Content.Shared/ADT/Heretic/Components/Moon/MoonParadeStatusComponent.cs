using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonParadeStatusComponent : Component
{
    [DataField]
    public float DamageSustained;

    [DataField]
    public float ReleaseThreshold = 50f;

    [DataField]
    public NetEntity? Parade;
}
