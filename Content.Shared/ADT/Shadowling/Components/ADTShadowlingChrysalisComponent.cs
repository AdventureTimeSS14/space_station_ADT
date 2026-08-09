using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingChrysalisComponent : Component
{
    [DataField]
    public EntityUid? Shadowling;
}
