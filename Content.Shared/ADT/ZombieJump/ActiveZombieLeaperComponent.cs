using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ZombieJump;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveZombieLeaperComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownDuration;
}
