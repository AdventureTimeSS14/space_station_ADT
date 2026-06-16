using Content.Shared.ADT.Trail;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.KereznikovImplant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KereznikovActiveComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan EndTime;

    [AutoNetworkedField]
    public float MovementSpeedModifier = 1.8f;

    [AutoNetworkedField]
    public float AttackSpeedModifier = 1.8f;

    public TrailComponent? Trail;
    public float ColorAccumulator;
}
