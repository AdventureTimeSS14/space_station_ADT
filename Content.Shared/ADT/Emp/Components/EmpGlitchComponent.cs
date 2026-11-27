using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpGlitchComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DurationMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float MotionStrength = 6f;

    [DataField, AutoNetworkedField]
    public float Smear = 0.6f;

    [DataField, AutoNetworkedField]
    public float Glitch = 0.5f;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan GlitchStart;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan GlitchEnd;

    public override bool SendOnlyToOwner => true;
}
