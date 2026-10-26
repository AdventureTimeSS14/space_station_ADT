using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Stealth;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcedStealthStatusEffectComponent : Component
{
    [DataField]
    public float Visibility = 0f;
}