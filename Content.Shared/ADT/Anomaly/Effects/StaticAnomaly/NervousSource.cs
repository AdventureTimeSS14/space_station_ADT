using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Anomaly.Effects.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NervousSourceComponent : Component
{
    [DataField]
    public float NervousRange = 7;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextEmote = TimeSpan.MaxValue;
}
