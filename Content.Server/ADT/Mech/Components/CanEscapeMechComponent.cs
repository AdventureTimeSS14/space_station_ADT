using Content.Shared.DoAfter;

namespace Content.Server.ADT.Mech.Components;

[RegisterComponent]
public sealed partial class CanEscapeMechComponent : Component
{
    public bool IsEscaping => DoAfter != null;

    [DataField("doAfter")]
    public DoAfterId? DoAfter;
}
