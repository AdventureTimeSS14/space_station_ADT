using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpOnCollideComponent : Component
{
    /// <summary>
    /// The amount of energy consumed by the EMP pulse. In Joules.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyConsumption = 80000f;

    /// <summary>
    /// The duration of the EMP disable effect in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DisableDuration = 5f;
}
