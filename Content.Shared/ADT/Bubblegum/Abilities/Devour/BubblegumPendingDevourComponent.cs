using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumPendingDevourComponent : Component
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public EntityUid Target;
}
