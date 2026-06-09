using Robust.Shared.GameStates;

namespace Content.Shared.ADT.SecondHeartImplant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecondHeartVisionComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan StartTime;

    [AutoNetworkedField]
    public TimeSpan EndTime;
}
