namespace Content.Shared.ADT.Mirror;

[RegisterComponent]
public sealed partial class MirrorComponent : Component
{
    [DataField]
    public Angle DirRotation = Angle.FromDegrees(90f);

    [DataField]
    public float GatherOffset = 1f;

    [DataField]
    public float ReflectionOffset = 0.2f;

    [DataField]
    public float FadeFactor = 1f;

    [DataField]
    public float ToleratedDistance = 1f;
}
