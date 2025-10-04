using Robust.Shared.GameStates;

namespace Content.Shared._SD.RMC.Humanoid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidRepresentationOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId? Species;

    [DataField, AutoNetworkedField]
    public LocId? Age;
}
