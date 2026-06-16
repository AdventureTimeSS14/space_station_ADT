using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.ImplantActivationVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantActivationVisionComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan StartTime;

    [AutoNetworkedField]
    public TimeSpan EndTime;
}
