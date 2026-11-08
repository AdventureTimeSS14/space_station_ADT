using Robust.Shared.GameStates;

namespace Content.Shared.ADT.RoleIntro;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTRoleIntroComponent : Component
{
    [DataField(required: true)]
    public LocId Title;

    [DataField(required: true)]
    public LocId Text;

    [DataField]
    public TimeSpan LockTime = TimeSpan.FromSeconds(30);
}
