using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.GodEye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTGodEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Double;

    [DataField]
    public string DoublePrefix = "double";

    [DataField]
    public LocId DoubleDescription = "adt-god-eye-double-desc";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGodEyeVisionComponent : Component
{
    [ViewVariables]
    public bool PreviousDrawFov = true;
}

[Serializable, NetSerializable]
public enum ADTGodEyeVisuals : byte
{
    Double,
}
