using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonConvertedComponent : Component
{
    [DataField]
    public float DamageSustained;

    [DataField]
    public float ReleaseThreshold = 75f;
}
