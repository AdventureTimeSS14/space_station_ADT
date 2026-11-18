using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Emp;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpGlitchComponent : Component
{
    [DataField]
    public float DurationMultiplier = 1f;

    [DataField]
    public float MotionStrength = 6f;

    [DataField]
    public float Smear = 0.6f;

    [DataField]
    public float Glitch = 0.5f;
}
