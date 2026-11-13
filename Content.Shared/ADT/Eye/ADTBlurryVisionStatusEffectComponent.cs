using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Eye;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTBlurryVisionStatusEffectComponent : Component
{
    [DataField]
    public float Blur = 3f;

    [ViewVariables]
    public bool Removed;
}
