using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.SecondHeartImplant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecondHeartVisionComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan StartTime;

    [AutoNetworkedField]
    public TimeSpan EndTime;
}
