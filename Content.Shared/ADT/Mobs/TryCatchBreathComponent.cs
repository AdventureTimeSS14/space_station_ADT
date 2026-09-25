using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TryCatchBreathComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DoAfterTime = 6f;

    [DataField]
    public string AudioPath = "/Audio/ADT/Alerts/CatchBreath/";
}
