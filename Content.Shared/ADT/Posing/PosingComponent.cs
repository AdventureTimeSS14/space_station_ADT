using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Posing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PosingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Angle CurrentAngle = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLimits = new(1f, 1f);

    [DataField("angleLimits"), AutoNetworkedField]
    private float _angleLimitsDegrees = 45f;

    [ViewVariables(VVAccess.ReadOnly)]
    public Angle AngleLimits => Angle.FromDegrees(_angleLimitsDegrees);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Posing = false;
}
