using Robust.Shared.GameObjects;

namespace Content.Server.ADT.BerserkImplant;

[RegisterComponent]
public sealed partial class BerserkImplantUserComponent : Component
{
    public string ActionProto = "ActionActivateBerserkImplant";
    public EntityUid? ActionUid;
}
