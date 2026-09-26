using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mirror;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MirrorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle DirRotation = Angle.FromDegrees(90f);

    [DataField, AutoNetworkedField]
    public float GatherOffset = 1f;

    [DataField, AutoNetworkedField]
    public float ReflectionOffset = 0.2f;

    [DataField, AutoNetworkedField]
    public float FadeFactor = 1f;

    [DataField, AutoNetworkedField]
    public float ToleratedDistance = 1f;
}
